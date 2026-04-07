namespace IpBlocker.API.Models.Responses;


public class CountryResponse
{
    public string CountryCode { get; set; } = string.Empty;
    public string CountryName { get; set; } = string.Empty;

    public DateTime BlockedAt { get; set; }
}