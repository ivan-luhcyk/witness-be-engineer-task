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

    [HttpGet("results")]
    public async Task<IActionResult> GetAllResults(CancellationToken cancellationToken)
    {
        var results = await _cache.GetAllResultsAsync();
        return Ok(results);
    }

    [HttpGet("{titleNumber}")]
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
