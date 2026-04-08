using IpBlocker.API.ExternalServices;
using IpBlocker.API.Models.Responses;
using IpBlocker.API.Services;


namespace IpBlocker.API.Services;

/// <summary>
/// Handles IP geolocation lookup and the check-block pipeline.
/// Orchestrates: GeoLocationApiClient → CountryService → LogService
/// DI Lifetime: Scoped.
/// </summary>
public class IpService : IIpService
{
    private readonly IGeoLocationApiClient _geoClient;
    private readonly ICountryService _countryService;
    private readonly ILogService _logService;
    private readonly ILogger<IpService> _logger;

    public IpService(
        IGeoLocationApiClient geoClient,
        ICountryService countryService,
        ILogService logService,
        ILogger<IpService> logger)
    {
        _geoClient = geoClient;
        _countryService = countryService;
        _logService = logService;
        _logger = logger;
    }

    public async Task<ServiceResult<IpLookupResponse>> LookupIpAsync(string ipAddress)
    {
        _logger.LogInformation("Looking up IP: {IpAddress}", ipAddress);

        var geoData = await _geoClient.GetLocationAsync(ipAddress);

        if (geoData == null)
        {
            return ServiceResult<IpLookupResponse>.ExternalError(
                $"Could not retrieve geolocation data for IP '{ipAddress}'. " +
                "The IP may be invalid, private, or the service may be unavailable.");
        }

        var response = new IpLookupResponse
        {
            IpAddress = geoData.Ip ?? ipAddress,
            CountryCode = geoData.CountryCode ?? "Unknown",
            CountryName = geoData.CountryName ?? "Unknown",
            City = geoData.City ?? string.Empty,
            Region = geoData.Region ?? string.Empty,
            Isp = geoData.Org ?? string.Empty,
            Latitude = geoData.Latitude,
            Longitude = geoData.Longitude
        };

        return ServiceResult<IpLookupResponse>.Success(response);
    }

    public async Task<IpCheckResponse> CheckIfBlockedAsync(string ipAddress, string userAgent)
    {
        _logger.LogInformation("Check-block pipeline started for IP: {IpAddress}", ipAddress);

        var countryCode = "Unknown";
        var countryName = "Unknown";
        var isBlocked = false;

        // Step 1: Call geolocation API to resolve country from IP
        var geoData = await _geoClient.GetLocationAsync(ipAddress);

        if (geoData != null && !string.IsNullOrWhiteSpace(geoData.CountryCode))
        {
            countryCode = geoData.CountryCode;
            countryName = geoData.CountryName ?? countryCode;

            // Step 2: Check if this country is blocked (permanent or temporal)
            isBlocked = _countryService.IsBlocked(countryCode);
        }
        else
        {
            _logger.LogWarning("Could not resolve country for IP {IpAddress}. Defaulting to not blocked.", ipAddress);
        }

        // Step 3: Log the attempt regardless of outcome
        // We log BOTH blocked and non-blocked — it's an audit log, not just an error log
        await _logService.LogAttemptAsync(new Models.Entities.BlockAttemptLog
        {
            IpAddress = ipAddress,
            Timestamp = DateTime.UtcNow,
            CountryCode = countryCode,
            CountryName = countryName,
            IsBlocked = isBlocked,
            UserAgent = userAgent
        });

        // Step 4: Build response
        var message = isBlocked
            ? $"Access denied: {countryName} ({countryCode}) is in the blocked list."
            : $"Access granted: {countryName} ({countryCode}) is not blocked.";

        _logger.LogInformation("Check-block result for {IpAddress}: IsBlocked={IsBlocked}", ipAddress, isBlocked);

        return new IpCheckResponse
        {
            IpAddress = ipAddress,
            CountryCode = countryCode,
            CountryName = countryName,
            IsBlocked = isBlocked,
            Message = message,
            CheckedAt = DateTime.UtcNow
        };
    }
}
