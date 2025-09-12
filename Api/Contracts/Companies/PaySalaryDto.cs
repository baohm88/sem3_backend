using System.ComponentModel.DataAnnotations;

namespace Api.Contracts.Companies;

/// <summary>
/// Payload for paying a driver's salary from a company wallet.
/// </summary>
public class PaySalaryDto
{
  /// <summary>
  /// Target driver's user identifier.
  /// </summary>
  /// <example>68a000000000000000000005</example>
  [Required]
  public string DriverUserId { get; set; } = default!;

  /// <summary>
  /// Amount in minor units (cents).
  /// Must be a positive integer.
  /// </summary>
  /// <example>150000</example>
  [Range(1, int.MaxValue)]
  public int AmountCents { get; set; }

  /// <summary>
  /// Optional idempotency key to prevent duplicate payments.
  /// </summary>
  /// <example>salary-2025-08-30-01</example>
  public string? IdempotencyKey { get; set; }

  /// <summary>
  /// Salary period in "YYYY-MM" format (optional, defaults to the current UTC month).
  /// </summary>
  /// <example>2025-08</example>
  public string? Period { get; set; }

  /// <summary>
  /// Optional note or memo for this payment.
  /// </summary>
  /// <example>August base salary</example>
  public string? Note { get; set; }
}
