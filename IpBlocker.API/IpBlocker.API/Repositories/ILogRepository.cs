using IpBlocker.API.Models.Entities;

namespace IpBlocker.API.Repositories;

public interface ILogRepository
{

    void Add(BlockAttemptLog log);
    IEnumerable<BlockAttemptLog> GetAll();
}