using System.Text.Json;
using IpBlocker.API.Common;
using Microsoft.Extensions.Options;

namespace IpBlocker.API.ExternalServices;
public class GeoLocationApiClient : IGeoLocationApiClient
{
    private readonly HttpClient _httpClient;
    private readonly GeoLocationApiSettings _settings;
    private readonly ILogger<GeoLocationApiClient> _logger;


    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public GeoLocationApiClient(
        HttpClient httpClient,
        IOptions<GeoLocationApiSettings> settings,
        ILogger<GeoLocationApiClient> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;

    
        _httpClient.BaseAddress = new Uri(_settings.BaseUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(10); // Don't wait forever
    }

    public async Task<GeoLocationApiResponse?> GetLocationAsync(
        string ipAddress,
        CancellationToken ct = default)
    {
        try
        {
         
            var url = string.IsNullOrWhiteSpace(_settings.ApiKey)
                ? $"{ipAddress}/json/"
                : $"{ipAddress}/json/?key={_settings.ApiKey}";

            _logger.LogInformation("Calling ipapi.co for IP: {IpAddress}", ipAddress);

            var response = await _httpClient.GetAsync(url, ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "ipapi.co returned {StatusCode} for IP {IpAddress}",
                    response.StatusCode, ipAddress);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync(ct);

            var result = JsonSerializer.Deserialize<GeoLocationApiResponse>(content, _jsonOptions);

           
            if (result?.Error == true)
            {
                _logger.LogWarning(
                    "ipapi.co reported error for IP {IpAddress}: {Reason}",
                    ipAddress, result.Reason);
                return null;
            }

            return result;
        }
        catch (TaskCanceledException ex) when (!ex.CancellationToken.IsCancellationRequested)
        {
            // Timeout — the request took longer than 10 seconds
            _logger.LogError("ipapi.co request timed out for IP: {IpAddress}", ipAddress);
            return null;
        }
        catch (HttpRequestException ex)
        {
            // Network-level failure (DNS, connection refused, etc.)
            _logger.LogError(ex, "HTTP error calling ipapi.co for IP: {IpAddress}", ipAddress);
            return null;
        }
        catch (Exception ex)
        {
            // Catch-all — never let external API failures crash our application
            _logger.LogError(ex, "Unexpected error calling ipapi.co for IP: {IpAddress}", ipAddress);
            return null;
        }
    }
}