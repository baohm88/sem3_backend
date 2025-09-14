namespace Api.Contracts.Companies;

public class CompanyReviewListItemDto
{
  public string Id { get; set; } = default!;
  public int Rating { get; set; }
  public string? Comment { get; set; }
  public DateTime CreatedAt { get; set; }

  // who left it
  public string UserId { get; set; } = default!;
  public string? UserName { get; set; }   // Rider name if we can find one
  public string? UserImgUrl { get; set; } // Rider avatar if available
}

public class CreateCompanyReviewDto
{
  public int Rating { get; set; }          // 1..5
  public string? Comment { get; set; }     // optional
}
