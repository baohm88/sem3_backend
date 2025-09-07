namespace Api.Contracts.Companies;

public class CompanyDriverDto
{
  public string UserId { get; set; } = default!;
  public string FullName { get; set; } = default!;
  public string? Phone { get; set; }
  public string? Bio { get; set; }
  public string? ImgUrl { get; set; }
  public decimal Rating { get; set; }
  public string? Skills { get; set; }
  public string? Location { get; set; }
  public bool IsAvailable { get; set; }
  public DateTime JoinedAt { get; set; }
  public int BaseSalaryCents { get; set; }
}