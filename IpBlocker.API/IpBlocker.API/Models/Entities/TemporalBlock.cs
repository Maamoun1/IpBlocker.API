namespace IpBlocker.API.Models.Entities;

public class TemporalBlock
{
    public string CountryCode { get; set; } = string.Empty;

    public string CountryName { get; set; } = string.Empty;

    public DateTime BlockedAt { get; set; } = DateTime.UtcNow;

    public DateTime ExpiresAt { get; set; }

    public int DurationMinutes { get; set; }

    public bool IsExpired => DateTime.UtcNow > ExpiresAt;
}