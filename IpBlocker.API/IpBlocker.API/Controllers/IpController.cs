using IpBlocker.API.Common.Helpers;
using IpBlocker.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace IpBlocker.API.Controllers;

[ApiController]
[Route("api/ip")]
[Produces("application/json")]
public class IpController : ControllerBase
{
    private readonly IIpService _ipService;
    private readonly ILogger<IpController> _logger;

    public IpController(IIpService ipService, ILogger<IpController> logger)
    {
        _ipService = ipService;
        _logger = logger;
    }

    // ─── Endpoint 4: IP Lookup ───────────────────────────────────────────────

    [HttpGet("lookup")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> LookupIp([FromQuery] string? ipAddress = null)
    {
        string targetIp;

        if (string.IsNullOrWhiteSpace(ipAddress))
        {
            // No IP provided — use the caller's IP from HttpContext
            targetIp = IpHelper.GetCallerIpAddress(HttpContext);
            _logger.LogInformation("No IP provided — using caller IP: {CallerIp}", targetIp);
        }
        else
        {
            // Validate the format before calling the external API
            if (!IpHelper.IsValidIpAddress(ipAddress))
                return BadRequest(new { message = $"'{ipAddress}' is not a valid IP address format." });

            targetIp = ipAddress;
        }

        var result = await _ipService.LookupIpAsync(targetIp);

        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, new { message = result.ErrorMessage });

        return Ok(result.Data);
    }

    // ─── Endpoint 5: Check If Caller Is Blocked ──────────────────────────────


    [HttpGet("check-block")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> CheckBlock()
    {
        // Extract caller IP from HttpContext (handles proxies via X-Forwarded-For)
        var callerIp = IpHelper.GetCallerIpAddress(HttpContext);

        // Extract User-Agent — "Unknown" if header is absent
        var userAgent = HttpContext.Request.Headers["User-Agent"].FirstOrDefault() ?? "Unknown";

        _logger.LogInformation(
            "Check-block requested. CallerIP: {CallerIp}, UserAgent: {UserAgent}",
            callerIp, userAgent);

        var result = await _ipService.CheckIfBlockedAsync(callerIp, userAgent);

        return Ok(result);
    }
}
