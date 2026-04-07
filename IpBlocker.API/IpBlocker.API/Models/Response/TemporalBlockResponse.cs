namespace IpBlocker.API.Models.Responses;

public class TemporalBlockResponse
{
    public string CountryCode { get; set; } = string.Empty;
    public string CountryName { get; set; } = string.Empty;
    public DateTime BlockedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public int DurationMinutes { get; set; }

    public int RemainingMinutes =>
        Math.Max(0, (int)(ExpiresAt - DateTime.UtcNow).TotalMinutes);
}