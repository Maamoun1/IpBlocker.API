using IpBlocker.API.Models.Entities;
using IpBlocker.API.Models.Responses;
using IpBlocker.API.Repositories;

namespace IpBlocker.API.Services;


public class LogService : ILogService
{
    private readonly ILogRepository _logRepo;
    private readonly ILogger<LogService> _logger;

    public LogService(ILogRepository logRepo, ILogger<LogService> logger)
    {
        _logRepo = logRepo;
        _logger = logger;
    }

    public Task LogAttemptAsync(BlockAttemptLog log)
    {
        _logRepo.Add(log);
        _logger.LogInformation(
            "Logged attempt: IP={IpAddress}, Country={CountryCode}, Blocked={IsBlocked}",
            log.IpAddress, log.CountryCode, log.IsBlocked);
        return Task.CompletedTask;
    }

    public Task<PagedResult<BlockAttemptLogResponse>> GetLogsAsync(int page, int pageSize)
    {
        var all = _logRepo.GetAll()
            .OrderByDescending(l => l.Timestamp)   // Newest first
            .ToList();

        var totalCount = all.Count;

        var items = all
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new BlockAttemptLogResponse
            {
                Id = l.Id,
                IpAddress = l.IpAddress,
                Timestamp = l.Timestamp,
                CountryCode = l.CountryCode,
                CountryName = l.CountryName,
                IsBlocked = l.IsBlocked,
                UserAgent = l.UserAgent
            })
            .ToList();

        return Task.FromResult(new PagedResult<BlockAttemptLogResponse>
        {
            Data = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        });
    }
}

