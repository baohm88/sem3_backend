using System.ComponentModel.DataAnnotations;

namespace Api.Contracts.Companies;

public class UpdateCompanyDto
{
    /// <example>AutoPro Co</example>
    [StringLength(200)]
    public string? Name { get; set; }

    /// <example>Logistics and last-mile delivery</example>
    public string? Description { get; set; }

    /// <example>https://cdn.example.com/logo.png</example>
    [Url]
    public string? ImgUrl { get; set; }

    /// <example>true</example>
    public bool? IsActive { get; set; }
}
