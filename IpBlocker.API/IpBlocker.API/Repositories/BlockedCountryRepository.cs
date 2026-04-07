using System.Collections.Concurrent;
using IpBlocker.API.Models.Entities;

namespace IpBlocker.API.Repositories;

public class BlockedCountryRepository : IBlockedCountryRepository
{

    private readonly ConcurrentDictionary<string, BlockedCountry> _store
        = new(StringComparer.OrdinalIgnoreCase);

    public bool TryAdd(BlockedCountry country)
    {
        return _store.TryAdd(country.CountryCode.ToUpperInvariant(), country);
    }

    public bool TryRemove(string countryCode)
    {
        return _store.TryRemove(countryCode.ToUpperInvariant(), out _);
    }
        
    public bool Exists(string countryCode)
    {
        return _store.ContainsKey(countryCode.ToUpperInvariant());
    }
    public IEnumerable<BlockedCountry> GetAll()
    {
        return _store.Values;
    }
    public BlockedCountry? GetByCode(string countryCode)
    {
        _store.TryGetValue(countryCode.ToUpperInvariant(), out var country);
        return country; 
    }
}