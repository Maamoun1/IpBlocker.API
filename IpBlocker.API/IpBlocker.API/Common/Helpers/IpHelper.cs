using System.Net;

namespace IpBlocker.API.Common.Helpers;

public static class IpHelper
{
   
    public static bool IsValidIpAddress(string? ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
            return false;

        return IPAddress.TryParse(ipAddress, out _);
    }

    public static string GetCallerIpAddress(HttpContext httpContext)
    {
        // 1. X-Forwarded-For (most common proxy header)
        var forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(forwardedFor))
        {
            var firstIp = forwardedFor.Split(',')[0].Trim();
            if (IsValidIpAddress(firstIp))
                return firstIp;
        }

        // 2. X-Real-IP (nginx-specific)
        var realIp = httpContext.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(realIp) && IsValidIpAddress(realIp))
            return realIp;

        // 3. Direct connection IP
        var remoteIp = httpContext.Connection.RemoteIpAddress?.ToString();
        if (!string.IsNullOrWhiteSpace(remoteIp))
        {
            // ::1 is IPv6 loopback — map to familiar 127.0.0.1
            if (remoteIp == "::1") return "127.0.0.1";
            return remoteIp;
        }

        return "Unknown";
    }
}