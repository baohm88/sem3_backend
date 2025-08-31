
using System.ComponentModel.DataAnnotations;

namespace Api.Contracts.Drivers;

public class ApplyCompanyDto
{
    /// <example>68a000000000000000000010</example>
    [Required]
    public string? CompanyId { get; set; }

    /// <example>2025-12-31T00:00:00Z</example>
    public DateTime? ExpiresAt { get; set; }
}
