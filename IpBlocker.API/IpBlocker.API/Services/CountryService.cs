using IpBlocker.API.ExternalServices;
using IpBlocker.API.Models.Entities;
using IpBlocker.API.Models.Requests;
using IpBlocker.API.Models.Responses;
using IpBlocker.API.Repositories;

namespace IpBlocker.API.Services;

/// <summary>
/// Implements all country blocking business logic.
/// Depends on repositories (storage) and geo client (name resolution).
/// DI Lifetime: Scoped.
/// </summary>
public class CountryService : ICountryService
{
    private readonly IBlockedCountryRepository _blockedRepo;
    private readonly ITemporalBlockRepository _temporalRepo;
    private readonly IGeoLocationApiClient _geoClient;
    private readonly ILogger<CountryService> _logger;

    public CountryService(
        IBlockedCountryRepository blockedRepo,
        ITemporalBlockRepository temporalRepo,
        IGeoLocationApiClient geoClient,
        ILogger<CountryService> logger)
    {
        _blockedRepo = blockedRepo;
        _temporalRepo = temporalRepo;
        _geoClient = geoClient;
        _logger = logger;
    }

    public async Task<ServiceResult<CountryResponse>> BlockCountryAsync(BlockCountryRequest request)
    {
        var code = request.CountryCode.ToUpperInvariant();
        _logger.LogInformation("Attempting to block country: {CountryCode}", code);

        var countryName = await ResolveCountryNameAsync(code);

        var entity = new BlockedCountry
        {
            CountryCode = code,
            CountryName = countryName,
            BlockedAt = DateTime.UtcNow
        };

        // TryAdd is atomic — thread-safe check-and-insert in one operation
        if (!_blockedRepo.TryAdd(entity))
        {
            _logger.LogWarning("Country {CountryCode} is already blocked.", code);
            return ServiceResult<CountryResponse>.Conflict(
                $"Country '{code}' is already in the blocked list.");
        }

        _logger.LogInformation("Country {CountryCode} ({CountryName}) blocked successfully.", code, countryName);
        return ServiceResult<CountryResponse>.Success(MapToResponse(entity), statusCode: 201);
    }

    public Task<ServiceResult<bool>> UnblockCountryAsync(string countryCode)
    {
        var code = countryCode.ToUpperInvariant();
        if (!_blockedRepo.TryRemove(code))
        {
            _logger.LogWarning("Attempted to unblock {CountryCode} but it was not found.", code);
            return Task.FromResult(
                ServiceResult<bool>.NotFound($"Country '{code}' is not in the blocked list."));
        }

        _logger.LogInformation("Country {CountryCode} unblocked successfully.", code);
        return Task.FromResult(ServiceResult<bool>.Success(true));
    }

    public Task<PagedResult<CountryResponse>> GetBlockedCountriesAsync(
        int page, int pageSize, string? search)
    {
        var query = _blockedRepo.GetAll().AsEnumerable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(c =>
                c.CountryCode.Contains(search.Trim(), StringComparison.OrdinalIgnoreCase) ||
                c.CountryName.Contains(search.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        var totalCount = query.Count();

        var items = query
            .OrderBy(c => c.CountryCode)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(MapToResponse)
            .ToList();

        return Task.FromResult(new PagedResult<CountryResponse>
        {
            Data = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        });
    }

    public bool IsBlocked(string countryCode)
    {
        var code = countryCode.ToUpperInvariant();
        // Blocked if permanently blocked OR has an active (non-expired) temporal block
        return _blockedRepo.Exists(code) || _temporalRepo.IsTemporallyBlocked(code);
    }

    public async Task<ServiceResult<TemporalBlockResponse>> TemporalBlockAsync(TemporalBlockRequest request)
    {
        var code = request.CountryCode.ToUpperInvariant();

        _logger.LogInformation("Attempting temporal block for {CountryCode} for {Minutes} minutes.", code, request.DurationMinutes);

        if (_temporalRepo.IsTemporallyBlocked(code))
        {
            return ServiceResult<TemporalBlockResponse>.Conflict(
                $"Country '{code}' is already temporarily blocked.");
        }

        var countryName = await ResolveCountryNameAsync(code);
        var now = DateTime.UtcNow;

        var entity = new TemporalBlock
        {
            CountryCode = code,
            CountryName = countryName,
            BlockedAt = now,
            ExpiresAt = now.AddMinutes(request.DurationMinutes),
            DurationMinutes = request.DurationMinutes
        };

        if (!_temporalRepo.TryAdd(entity))
        {
            return ServiceResult<TemporalBlockResponse>.Conflict(
                $"Country '{code}' is already temporarily blocked.");
        }

        _logger.LogInformation("Country {CountryCode} temporarily blocked until {ExpiresAt}.", code, entity.ExpiresAt);
        return ServiceResult<TemporalBlockResponse>.Success(MapToTemporalResponse(entity), statusCode: 201);
    }

    // ─── Private Helpers ─────────────────────────────────────────────────────

    private Task<string> ResolveCountryNameAsync(string countryCode)
    {
        // Check already-stored countries first (free — no API call)
        var existing = _blockedRepo.GetByCode(countryCode);
        if (existing != null && !string.IsNullOrWhiteSpace(existing.CountryName))
            return Task.FromResult(existing.CountryName);

        // Use static ISO map — fast, offline, no API call needed for name resolution
        if (CountryNameMap.TryGetValue(countryCode.ToUpperInvariant(), out var name))
            return Task.FromResult(name);

        return Task.FromResult(countryCode.ToUpperInvariant());
    }

    private static CountryResponse MapToResponse(BlockedCountry e) => new()
    {
        CountryCode = e.CountryCode,
        CountryName = e.CountryName,
        BlockedAt = e.BlockedAt
    };

    private static TemporalBlockResponse MapToTemporalResponse(TemporalBlock e) => new()
    {
        CountryCode = e.CountryCode,
        CountryName = e.CountryName,
        BlockedAt = e.BlockedAt,
        ExpiresAt = e.ExpiresAt,
        DurationMinutes = e.DurationMinutes
    };

    private static readonly Dictionary<string, string> CountryNameMap = new()
    {
        ["AF"] = "Afghanistan",
        ["AL"] = "Albania",
        ["DZ"] = "Algeria",
        ["AR"] = "Argentina",
        ["AU"] = "Australia",
        ["AT"] = "Austria",
        ["BE"] = "Belgium",
        ["BR"] = "Brazil",
        ["CA"] = "Canada",
        ["CN"] = "China",
        ["CO"] = "Colombia",
        ["HR"] = "Croatia",
        ["CZ"] = "Czech Republic",
        ["DK"] = "Denmark",
        ["EG"] = "Egypt",
        ["ET"] = "Ethiopia",
        ["FI"] = "Finland",
        ["FR"] = "France",
        ["DE"] = "Germany",
        ["GH"] = "Ghana",
        ["GR"] = "Greece",
        ["HU"] = "Hungary",
        ["IN"] = "India",
        ["ID"] = "Indonesia",
        ["IR"] = "Iran",
        ["IQ"] = "Iraq",
        ["IE"] = "Ireland",
        ["IL"] = "Israel",
        ["IT"] = "Italy",
        ["JP"] = "Japan",
        ["JO"] = "Jordan",
        ["KE"] = "Kenya",
        ["KW"] = "Kuwait",
        ["LB"] = "Lebanon",
        ["LY"] = "Libya",
        ["MY"] = "Malaysia",
        ["MX"] = "Mexico",
        ["MA"] = "Morocco",
        ["NL"] = "Netherlands",
        ["NZ"] = "New Zealand",
        ["NG"] = "Nigeria",
        ["NO"] = "Norway",
        ["PK"] = "Pakistan",
        ["PS"] = "Palestine",
        ["PH"] = "Philippines",
        ["PL"] = "Poland",
        ["PT"] = "Portugal",
        ["QA"] = "Qatar",
        ["RO"] = "Romania",
        ["RU"] = "Russia",
        ["SA"] = "Saudi Arabia",
        ["SN"] = "Senegal",
        ["ZA"] = "South Africa",
        ["KR"] = "South Korea",
        ["ES"] = "Spain",
        ["SE"] = "Sweden",
        ["CH"] = "Switzerland",
        ["SY"] = "Syria",
        ["TW"] = "Taiwan",
        ["TH"] = "Thailand",
        ["TN"] = "Tunisia",
        ["TR"] = "Turkey",
        ["UA"] = "Ukraine",
        ["AE"] = "United Arab Emirates",
        ["GB"] = "United Kingdom",
        ["US"] = "United States",
        ["VN"] = "Vietnam",
        ["YE"] = "Yemen",
    };
}