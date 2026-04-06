namespace IpBlocker.API.Models.Entities;

public class BlockAttemptLog
{

    public Guid Id { get; set; } = Guid.NewGuid();

    public string IpAddress { get; set; } = string.Empty;

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public string CountryCode { get; set; } = string.Empty;

    public string CountryName { get; set; } = string.Empty;

    public bool IsBlocked { get; set; }

    public string UserAgent { get; set; } = string.Empty;
}