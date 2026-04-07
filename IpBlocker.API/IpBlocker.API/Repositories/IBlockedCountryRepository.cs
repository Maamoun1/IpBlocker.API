using IpBlocker.API.Models.Entities;

namespace IpBlocker.API.Repositories;

public interface IBlockedCountryRepository
{
    bool TryAdd(BlockedCountry country);

    bool TryRemove(string countryCode);
    bool Exists(string countryCode);
    IEnumerable<BlockedCountry> GetAll();

    BlockedCountry? GetByCode(string countryCode);
}