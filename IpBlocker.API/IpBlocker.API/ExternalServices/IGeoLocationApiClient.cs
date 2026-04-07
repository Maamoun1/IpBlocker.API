namespace IpBlocker.API.ExternalServices;

public interface IGeoLocationApiClient
{
    Task<GeoLocationApiResponse?> GetLocationAsync(string ipAddress, CancellationToken ct = default);
}