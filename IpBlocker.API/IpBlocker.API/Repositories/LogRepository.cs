using System.Collections.Concurrent;
using IpBlocker.API.Models.Entities;

namespace IpBlocker.API.Repositories;

public class LogRepository : ILogRepository
{
   
    private readonly ConcurrentQueue<BlockAttemptLog> _logs = new();
    public void Add(BlockAttemptLog log)
    {
        _logs.Enqueue(log);
    }
    public IEnumerable<BlockAttemptLog> GetAll()
    {
        return _logs.ToArray();
    }
}