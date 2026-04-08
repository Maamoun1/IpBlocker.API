using IpBlocker.API.Models.Requests;
using IpBlocker.API.Models.Responses;

namespace IpBlocker.API.Services;

/// <summary>
/// Contract for all country blocking business logic.
/// Controllers depend on this interface, never on the concrete class.
/// </summary>
public interface ICountryService
{
    Task<ServiceResult<CountryResponse>> BlockCountryAsync(BlockCountryRequest request);
    Task<ServiceResult<bool>> UnblockCountryAsync(string countryCode);
    Task<PagedResult<CountryResponse>> GetBlockedCountriesAsync(int page, int pageSize, string? search);
    bool IsBlocked(string countryCode);
    Task<ServiceResult<TemporalBlockResponse>> TemporalBlockAsync(TemporalBlockRequest request);
}