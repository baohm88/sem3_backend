using System.ComponentModel.DataAnnotations;

namespace Api.Contracts.Companies
{
  /// <summary>
  /// Payload for withdrawing money from a company wallet.
  /// </summary>
  public class WithdrawDto
  {
    /// <summary>
    /// Amount in minor units (cents). Must be a positive integer.
    /// </summary>
    /// <example>50000</example>
    [Range(1, int.MaxValue)]
    public int AmountCents { get; set; }

    /// <summary>
    /// Idempotency key to prevent duplicate submissions.
    /// </summary>
    public string? IdempotencyKey { get; set; }
  }
}
