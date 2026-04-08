using IpBlocker.API.Models.Requests;
using IpBlocker.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace IpBlocker.API.Controllers;


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

    // ─── Endpoint 1: Add a Blocked Country ──────────────────────────────────

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

    // ─── Endpoint 2: Delete a Blocked Country ───────────────────────────────


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

    // ─── Endpoint 3: Get All Blocked Countries ───────────────────────────────


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

    // ─── Endpoint 7: Temporarily Block a Country ─────────────────────────────

 
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
