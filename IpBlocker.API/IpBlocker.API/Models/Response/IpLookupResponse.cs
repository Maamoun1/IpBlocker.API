namespace IpBlocker.API.Models.Responses;

public class IpLookupResponse
{
    public string IpAddress { get; set; } = string.Empty;

    public string CountryCode { get; set; } = string.Empty;

    public string CountryName { get; set; } = string.Empty;

    public string City { get; set; } = string.Empty;

    public string Region { get; set; } = string.Empty;

    public string Isp { get; set; } = string.Empty;

    public double? Latitude { get; set; }

    public double? Longitude { get; set; }
}