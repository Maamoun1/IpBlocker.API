using System.Collections.Concurrent;
using IpBlocker.API.Models.Entities;

namespace IpBlocker.API.Repositories;

public class TemporalBlockRepository : ITemporalBlockRepository
{
    private readonly ConcurrentDictionary<string, TemporalBlock> _store
        = new(StringComparer.OrdinalIgnoreCase);

    public bool TryAdd(TemporalBlock block)
    {
        return _store.TryAdd(block.CountryCode.ToUpperInvariant(), block);

    }

    public bool TryRemove(string countryCode)
    {
        return _store.TryRemove(countryCode.ToUpperInvariant(), out _);
    }

    public bool IsTemporallyBlocked(string countryCode)
    {
        if (_store.TryGetValue(countryCode.ToUpperInvariant(), out var block))
        {
        
            return !block.IsExpired;
        }

        return false;
    }

    public IEnumerable<TemporalBlock> GetAll()
    {
        return _store.Values;
    }

    public IEnumerable<TemporalBlock> GetExpired()
    {
      
        return _store.Values.Where(b => b.IsExpired);
    }
}