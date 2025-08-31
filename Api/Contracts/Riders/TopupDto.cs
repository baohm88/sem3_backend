
using System.ComponentModel.DataAnnotations;

namespace Api.Contracts.Riders;

public class TopupDto
{
    /// <example>200000</example>
    [Range(1, int.MaxValue)]
    public int AmountCents { get; set; }

    /// <example>rider-topup-2025-08-30-01</example>
    public string? IdempotencyKey { get; set; }
}
