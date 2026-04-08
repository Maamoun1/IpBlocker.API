using IpBlocker.API.Common.Helpers;
using IpBlocker.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace IpBlocker.API.Controllers;

/// <summary>
/// Handles IP geolocation lookup and block-check endpoints.
/// </summary>
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

    /// <summary>
    /// Looks up geolocation information for an IP address.
    /// If ipAddress is omitted, uses the caller's IP automatically.
    /// </summary>
    /// <param name="ipAddress">Optional IP to look up. Defaults to caller's IP.</param>
    /// <response code="200">Geolocation data returned successfully.</response>
    /// <response code="400">The provided IP address format is invalid.</response>
    /// <response code="502">Geolocation service is unavailable or returned an error.</response>
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

    /// <summary>
    /// Checks whether the caller's IP address originates from a blocked country.
    /// Always returns 200 — check IsBlocked in the response body.
    /// </summary>
    /// <response code="200">Check completed. See IsBlocked field in response.</response>

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
