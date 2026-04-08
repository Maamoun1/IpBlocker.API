using IpBlocker.API.Models.Entities;
using IpBlocker.API.Models.Responses;


namespace IpBlocker.API.Services;

public interface ILogService
{
    Task LogAttemptAsync(BlockAttemptLog log);
    Task<PagedResult<BlockAttemptLogResponse>> GetLogsAsync(int page, int pageSize);
}
