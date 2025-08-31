using System.ComponentModel.DataAnnotations;

namespace Api.Contracts.Companies;

public class TopupDto
{
    /// <example>500000</example>
    [Range(1, int.MaxValue)]
    public int AmountCents { get; set; }

    /// <example>topup-2025-08-30-01</example>
    public string? IdempotencyKey { get; set; }
}
