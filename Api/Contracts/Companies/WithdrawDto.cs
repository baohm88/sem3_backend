using System.ComponentModel.DataAnnotations;

namespace Api.Contracts.Companies
{
  public class WithdrawDto
  {
    /// <example>50000</example>
    [Range(1, int.MaxValue)]
    public int AmountCents { get; set; }

    /// <summary>Idempotency key để chống gửi trùng</summary>
    public string? IdempotencyKey { get; set; }
  }
}