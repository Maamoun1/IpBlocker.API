using IpBlocker.API.Common;
using Microsoft.Extensions.Options;
using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime;
using System.Text.Json;
using static System.Net.Mime.MediaTypeNames;

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

    }

    public async Task<GeoLocationApiResponse?> GetLocationAsync(
        string ipAddress,
        CancellationToken ct = default)
    {
        try
        {
            // BaseUrl in appsettings.json = "https://ipapi.co/"
            // Result for "8.8.8.8"       = "https://ipapi.co/8.8.8.8/json/"
            var baseUrl = _settings.BaseUrl.TrimEnd('/');
            var absoluteUrl = string.IsNullOrWhiteSpace(_settings.ApiKey)
                ? $"{baseUrl}/{ipAddress}/json/"
                : $"{baseUrl}/{ipAddress}/json/?key={_settings.ApiKey}";

            _logger.LogInformation(
                "Calling ipapi.co for IP: {IpAddress}, URL: {Url}",
                ipAddress, absoluteUrl);

            var response = await _httpClient.GetAsync(absoluteUrl, ct);

            // Handle known ipapi.co error status codes explicitly
            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                _logger.LogWarning(
                    "ipapi.co rate limit hit for IP {IpAddress}. Free tier = 1000 req/day.",
                    ipAddress);
                return null;
            }

            if (response.StatusCode == HttpStatusCode.Forbidden)
            {
                _logger.LogError(
                    "ipapi.co returned 403 Forbidden for IP {IpAddress}. " +
                    "Check your API key in appsettings.json → GeoLocationApi:ApiKey.",
                    ipAddress);
                return null;
            }

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "ipapi.co returned {StatusCode} for IP {IpAddress}",
                    (int)response.StatusCode, ipAddress);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync(ct);

            var result = JsonSerializer.Deserialize<GeoLocationApiResponse>(content, _jsonOptions);

            // ipapi.co returns HTTP 200 with error:true in the body for invalid IPs
            if (result?.Error == true)
            {
                _logger.LogWarning(
                    "ipapi.co reported error for IP {IpAddress}: {Reason}",
                    ipAddress, result.Reason);
                return null;
            }

            _logger.LogInformation(
                "Lookup succeeded: {IpAddress} → {CountryCode} ({CountryName})",
                ipAddress, result?.CountryCode, result?.CountryName);

            return result;
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            _logger.LogError(
                "ipapi.co request timed out for IP: {IpAddress}", ipAddress);
            return null;
        }
        catch (TaskCanceledException)
        {
            _logger.LogInformation(
                "Request cancelled by caller for IP: {IpAddress}", ipAddress);
            return null;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex,
                "Network error calling ipapi.co for IP {IpAddress}.", ipAddress);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unexpected error calling ipapi.co for IP {IpAddress}. Type: {Type}",
                ipAddress, ex.GetType().Name);
            return null;
        }
    }
}
