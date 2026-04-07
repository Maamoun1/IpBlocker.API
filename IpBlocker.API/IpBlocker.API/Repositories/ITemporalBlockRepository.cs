using IpBlocker.API.Models.Entities;

namespace IpBlocker.API.Repositories;

public interface ITemporalBlockRepository
{
    bool TryAdd(TemporalBlock block);

    bool TryRemove(string countryCode);

    bool IsTemporallyBlocked(string countryCode);

    IEnumerable<TemporalBlock> GetAll();

    IEnumerable<TemporalBlock> GetExpired();
}