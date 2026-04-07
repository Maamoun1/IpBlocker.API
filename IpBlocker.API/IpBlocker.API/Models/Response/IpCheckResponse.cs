namespace IpBlocker.API.Models.Responses;

// Always returns 200 OK — IsBlocked in the body carries the semantic meaning,
// not the HTTP status code (this endpoint is a check, not an enforcement gate).
public class IpCheckResponse
{
    public string IpAddress { get; set; } = string.Empty;

    public string CountryCode { get; set; } = string.Empty;

    public string CountryName { get; set; } = string.Empty;

    // True if the resolved country is in the blocked list (permanent OR temporal).
    // False if allowed through.
    public bool IsBlocked { get; set; }

    // Human-readable explanation.
    // Examples: "Access denied: Egypt is in the blocked list."
    //           "Access granted: United States is not blocked."
    public string Message { get; set; } = string.Empty;

    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
}