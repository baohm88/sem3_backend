using System.ComponentModel.DataAnnotations;

namespace Api.Contracts.Companies;

public class AddServiceDto
{
    /// <example>Bike Ride</example>
    [Required, StringLength(200)]
    public string? Title { get; set; }
    [Url]
    public string? ImgUrl { get; set; }

    /// <example>Short-distance city ride</example>
    public string? Description { get; set; }

    /// <example>30000</example>
    [Range(1, int.MaxValue)]
    public int PriceCents { get; set; }

    /// <example>true</example>
    public bool? IsActive { get; set; }
}
