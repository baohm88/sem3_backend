using System.ComponentModel.DataAnnotations;

namespace Api.Contracts.Companies;

public class PaySalaryDto
{
    /// <example>68a000000000000000000005</example>
    [Required]
    public string DriverUserId { get; set; } = default!;

    /// <example>150000</example>
    [Range(1, int.MaxValue)]
    public int AmountCents { get; set; }

    /// <example>salary-2025-08-30-01</example>
    public string? IdempotencyKey { get; set; }
}
