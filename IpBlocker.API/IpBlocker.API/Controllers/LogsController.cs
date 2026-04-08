using IpBlocker.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace IpBlocker.API.Controllers;


[ApiController]
[Route("api/logs")]
[Produces("application/json")]
public class LogsController : ControllerBase
{
    private readonly ILogService _logService;

    public LogsController(ILogService logService)
    {
        _logService = logService;
    }

    // ─── Endpoint 6: Get Blocked Attempt Logs ────────────────────────────────

    [HttpGet("blocked-attempts")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBlockedAttempts(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (page < 1) page = 1;
        pageSize = Math.Clamp(pageSize, 1, 100);

        var result = await _logService.GetLogsAsync(page, pageSize);
        return Ok(result);
    }
}
