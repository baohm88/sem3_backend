using System.ComponentModel.DataAnnotations;

namespace Api.Contracts.Companies;

public class PayMembershipDto
{
    /// <example>Basic</example>
    [Required, StringLength(50)]
    public string? Plan { get; set; }

    /// <example>99000</example>
    [Range(1, int.MaxValue)]
    public int AmountCents { get; set; }

    /// <example>membership-2025-09</example>
    public string? IdempotencyKey { get; set; }
}
