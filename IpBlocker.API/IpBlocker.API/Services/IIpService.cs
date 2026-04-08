using IpBlocker.API.Models.Responses;
using System.ComponentModel;

namespace IpBlocker.API.Services;

public interface IIpService
{
    Task<ServiceResult<IpLookupResponse>> LookupIpAsync(string ipAddress);
    Task<IpCheckResponse> CheckIfBlockedAsync(string ipAddress, string userAgent);
}
