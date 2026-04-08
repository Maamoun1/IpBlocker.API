using IpBlocker.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace IpBlocker.API.Controllers;

/// <summary>
/// Exposes the audit log of IP block-check attempts.
/// </summary>
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

    /// <summary>
    /// Returns a paginated list of all IP block-check attempts.
    /// Ordered by most recent first.
    /// Each entry includes IP, timestamp, country, blocked status, and User-Agent.
    /// </summary>
    /// <param name="page">Page number (1-based). Default: 1.</param>
    /// <param name="pageSize">Items per page (1–100). Default: 20.</param>
    /// <response code="200">Paginated list of log entries.</response>
  
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
