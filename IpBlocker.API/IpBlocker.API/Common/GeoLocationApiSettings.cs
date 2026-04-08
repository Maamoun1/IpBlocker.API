namespace IpBlocker.API.Common;


public class GeoLocationApiSettings
{
   
    public const string SectionName = "GeoLocationApi";

    public string BaseUrl { get; set; } = "https://ipapi.co/";
    public string ApiKey { get; set; } = string.Empty;
}