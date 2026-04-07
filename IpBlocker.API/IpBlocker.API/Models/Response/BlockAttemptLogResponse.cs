namespace IpBlocker.API.Models.Responses;

public class BlockAttemptLogResponse
{
    public Guid Id { get; set; }

    public string IpAddress { get; set; } = string.Empty;

    public DateTime Timestamp { get; set; }

    public string CountryCode { get; set; } = string.Empty;

    public string CountryName { get; set; } = string.Empty;

    public bool IsBlocked { get; set; }

    public string UserAgent { get; set; } = string.Empty;
}