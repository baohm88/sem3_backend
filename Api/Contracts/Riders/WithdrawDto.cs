
using System.ComponentModel.DataAnnotations;

namespace Api.Contracts.Riders;

public class WithdrawDto
{
    /// <example>50000</example>
    [Range(1, int.MaxValue)]
    public int AmountCents { get; set; }

    /// <example>rider-withdraw-2025-08-30-01</example>
    public string? IdempotencyKey { get; set; }
}
