using IpBlocker.API.Models.Requests;
using IpBlocker.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace IpBlocker.API.Controllers;

/// <summary>
/// Handles all country block management endpoints.
/// Endpoints 1, 2, 3, and 7 from the assignment.
///
/// [ApiController] gives us:
///   - Automatic model validation (returns 400 if Data Annotations fail)
///   - Automatic binding from body/route/query without [FromBody] everywhere
///   - ProblemDetails-formatted error responses
/// </summary>
[ApiController]
[Route("api/countries")]
[Produces("application/json")]
public class CountriesController : ControllerBase
{
    private readonly ICountryService _countryService;
    private readonly ILogger<CountriesController> _logger;

    public CountriesController(ICountryService countryService, ILogger<CountriesController> logger)
    {
        _countryService = countryService;
        _logger = logger;
    }

    /// <summary>
    /// Permanently blocks a country by its ISO 3166-1 alpha-2 code.
    /// </summary>
    /// <param name="request">Body containing the country code (e.g., "EG", "US").</param>
    /// <returns>The newly blocked country entry.</returns>
    /// <response code="201">Country successfully added to the blocked list.</response>
    /// <response code="400">Invalid country code format.</response>
    /// <response code="409">Country is already in the blocked list.</response>
    [HttpPost("block")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> BlockCountry([FromBody] BlockCountryRequest request)
    {
        // [ApiController] already handled model validation before reaching here.
        // If CountryCode was missing or wrong format, it returned 400 automatically.

        var result = await _countryService.BlockCountryAsync(request);

        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, new { message = result.ErrorMessage });

        return StatusCode(StatusCodes.Status201Created, result.Data);
    }

    /// <summary>
    /// Removes a country from the permanent blocked list.
    /// </summary>
    /// <param name="countryCode">The ISO country code to unblock (e.g., "EG").</param>
    /// <response code="204">Country successfully removed.</response>
    /// <response code="404">Country was not found in the blocked list.</response>

    [HttpDelete("block/{countryCode}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UnblockCountry([FromRoute] string countryCode)
    {
        if (string.IsNullOrWhiteSpace(countryCode))
            return BadRequest(new { message = "Country code is required." });

        var result = await _countryService.UnblockCountryAsync(countryCode);

        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, new { message = result.ErrorMessage });

        return NoContent(); // 204 — success with no response body
    }

    /// <summary>
    /// Returns a paginated, searchable list of all permanently blocked countries.
    /// </summary>
    /// <param name="page">Page number (1-based). Default: 1.</param>
    /// <param name="pageSize">Items per page (1–100). Default: 10.</param>
    /// <param name="search">Optional filter by country code or name.</param>
    /// <response code="200">Paginated list of blocked countries.</response>

    [HttpGet("blocked")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBlockedCountries(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null)
    {
        // Defensive clamping — even if the client sends page=0 or pageSize=999
        if (page < 1) page = 1;
        pageSize = Math.Clamp(pageSize, 1, 100);

        var result = await _countryService.GetBlockedCountriesAsync(page, pageSize, search);
        return Ok(result);
    }


    /// <summary>
    /// Temporarily blocks a country for a specified duration (1–1440 minutes).
    /// The block is automatically removed by a background service after expiry.
    /// </summary>
    /// <param name="request">Country code and duration in minutes.</param>
    /// <response code="201">Temporal block created successfully.</response>
    /// <response code="400">Invalid country code or duration out of range.</response>
    /// <response code="409">This country is already temporarily blocked.</response>

    [HttpPost("temporal-block")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> TemporalBlock([FromBody] TemporalBlockRequest request)
    {
        var result = await _countryService.TemporalBlockAsync(request);

        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, new { message = result.ErrorMessage });

        return StatusCode(StatusCodes.Status201Created, result.Data);
    }
}
