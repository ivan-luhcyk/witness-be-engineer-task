using LeaseApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using WitnessBackendEngineerTask.Common.Models;

namespace LeaseApi.Controllers;

[ApiController]
[Route("")]
[EnableRateLimiting("lease-get")]
public sealed class LeaseController : ControllerBase
{
    private readonly ILeaseCache _cache;
    private readonly IParseTrigger _parseTrigger;
    private readonly ILogger<LeaseController> _logger;

    public LeaseController(ILeaseCache cache, IParseTrigger parseTrigger, ILogger<LeaseController> logger)
    {
        _cache = cache;
        _parseTrigger = parseTrigger;
        _logger = logger;
    }

    /// <summary>
    /// Returns all parsed lease results currently available in Redis.
    /// </summary>
    /// <param name="cancellationToken">Request cancellation token.</param>
    /// <returns>Collection of parsed lease records.</returns>
    [HttpGet("results")]
    [ProducesResponseType(typeof(IReadOnlyList<ParsedScheduleNoticeOfLease>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllResults(CancellationToken cancellationToken)
    {
        var results = await _cache.GetAllResultsAsync();
        return Ok(results);
    }

    /// <summary>
    /// Gets a parsed lease by title number.
    /// </summary>
    /// <param name="titleNumber">Lease title number (for example: <c>TGL24029</c>).</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    /// <returns>
    /// <list type="bullet">
    /// <item><description><c>200</c> when parsed data is available.</description></item>
    /// <item><description><c>202</c> when parsing is queued/in progress.</description></item>
    /// <item><description><c>500</c> when parsing failed for this title.</description></item>
    /// </list>
    /// </returns>
    [HttpGet("{titleNumber}")]
    [ProducesResponseType(typeof(ParsedScheduleNoticeOfLease), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(LeaseProcessingStatus), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> GetByTitle(string titleNumber, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(titleNumber))
        {
            return BadRequest("Title number is required.");
        }

        var normalizedTitle = titleNumber.Trim().ToUpperInvariant();
        var result = await _cache.GetResultAsync(normalizedTitle);
        if (result is not null)
        {
            return Ok(result);
        }

        var existingStatus = await _cache.GetStatusAsync(normalizedTitle);
        if (existingStatus is null)
        {
            var pending = new LeaseProcessingStatus(normalizedTitle, LeaseProcessingStatus.PendingState, null);
            await _cache.SetStatusAsync(pending);

            var shouldTrigger = await _cache.TryAcquireParseTriggerLockAsync(TimeSpan.FromSeconds(15));
            if (shouldTrigger)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _parseTrigger.TriggerAsync(normalizedTitle, CancellationToken.None);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to trigger parse for title {TitleNumber}", normalizedTitle);
                    }
                }, CancellationToken.None);
            }

            return Accepted($"/{normalizedTitle}", pending);
        }

        if (string.Equals(existingStatus.Status, LeaseProcessingStatus.FailedState, StringComparison.OrdinalIgnoreCase))
        {
            return Problem(existingStatus.Error ?? "Lease processing failed.", statusCode: StatusCodes.Status500InternalServerError);
        }

        return Accepted($"/{normalizedTitle}", existingStatus);
    }
}
