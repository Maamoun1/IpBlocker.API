using System.ComponentModel.DataAnnotations;

namespace IpBlocker.API.Models.Requests;

public class TemporalBlockRequest
{

    [Required(ErrorMessage = "Country code is required.")]
    [StringLength(2, MinimumLength = 2, ErrorMessage = "Country code must be exactly 2 characters.")]
    [RegularExpression(@"^[a-zA-Z]+$", ErrorMessage = "Country code must contain letters only.")]
    public string CountryCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Duration is required.")]
    [Range(1, 1440, ErrorMessage = "Duration must be between 1 and 1440 minutes (24 hours).")]
    public int DurationMinutes { get; set; }
}