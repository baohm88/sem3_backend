namespace Api.Contracts.Drivers
{
  public class TopupDto
  {
    /// <example>50000</example>
    public int AmountCents { get; set; }

    /// <summary>Idempotency key để chống gửi trùng</summary>
    public string? IdempotencyKey { get; set; }
  }
}
