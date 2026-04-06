using System.ComponentModel.DataAnnotations;

namespace IpBlocker.API.Models.Requests;
public class BlockCountryRequest
{

    [Required(ErrorMessage = "Country code is required.")]
    [StringLength(2, MinimumLength = 2, ErrorMessage = "Country code must be exactly 2 characters.")]
    [RegularExpression(@"^[a-zA-Z]+$", ErrorMessage = "Country code must contain letters only.")]
    public string CountryCode { get; set; } = string.Empty;

}