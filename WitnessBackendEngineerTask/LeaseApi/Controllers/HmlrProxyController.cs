using LeaseApi.Clients;
using LeaseApi.Models;
using Microsoft.AspNetCore.Mvc;

namespace LeaseApi.Controllers;

[ApiController]
[Route("hmlr")]
public sealed class HmlrProxyController : ControllerBase
{
    private readonly IHmlrClient _hmlrClient;
    private readonly ILogger<HmlrProxyController> _logger;

    public HmlrProxyController(IHmlrClient hmlrClient, ILogger<HmlrProxyController> logger)
    {
        _hmlrClient = hmlrClient;
        _logger = logger;
    }

    [HttpGet("schedules")]
    public async Task<IActionResult> GetSchedules(CancellationToken cancellationToken)
    {
        try
        {
            var scheduleItems = await _hmlrClient.GetSchedulesAsync(cancellationToken);
            return Ok(scheduleItems);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "HMLR configuration error.");
            return Problem(ex.Message, statusCode: StatusCodes.Status500InternalServerError);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HMLR request failed.");
            return Problem(
                detail: ex.Message,
                statusCode: StatusCodes.Status502BadGateway,
                title: "HMLR request failed.");
        }
    }
}
