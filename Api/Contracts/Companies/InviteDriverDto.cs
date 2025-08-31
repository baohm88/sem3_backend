using System.ComponentModel.DataAnnotations;

namespace Api.Contracts.Companies;

public class InviteDriverDto
{
    /// <example>68a000000000000000000007</example>
    [Required]
    public string? DriverUserId { get; set; }

    /// <example>100000</example>
    [Range(0, int.MaxValue)]
    public int BaseSalaryCents { get; set; }

    /// <example>2025-12-31T00:00:00Z</example>
    public DateTime? ExpiresAt { get; set; }
}
