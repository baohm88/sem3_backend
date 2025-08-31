
using System.ComponentModel.DataAnnotations;

namespace Api.Contracts.Drivers;

public class WithdrawDto
{
    /// <example>100000</example>
    [Range(1, int.MaxValue)]
    public int AmountCents { get; set; }

    /// <example>withdraw-2025-08-30-01</example>
    public string? IdempotencyKey { get; set; }
}
