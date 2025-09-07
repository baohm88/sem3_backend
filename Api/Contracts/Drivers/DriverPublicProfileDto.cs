// Api/Contracts/Drivers/DriverPublicProfileDto.cs
using Domain.Entities;

namespace Api.Contracts.Drivers;

public class DriverPublicProfileDto
{
  public DriverProfile? Driver { get; set; }

  public CompanyBriefDto? HiredCompany { get; set; }

  public List<EmploymentDto> EmploymentHistory { get; set; } = new();

  public decimal Rating { get; set; } = 0;          // chưa có nguồn => để 0
  public List<ReviewDto> Reviews { get; set; } = new(); // chưa có nguồn => để trống
}

public class CompanyBriefDto
{
  public string Id { get; set; } = default!;
  public string Name { get; set; } = "";
  public string? ImgUrl { get; set; }
  public string Membership { get; set; } = "Free";
  public decimal Rating { get; set; } = 0;
}

public class EmploymentDto
{
  public string CompanyId { get; set; } = default!;
  public string CompanyName { get; set; } = "";
  public string? CompanyImgUrl { get; set; }
  public DateTime Since { get; set; }     // từ khi tạo relation
  public int BaseSalaryCents { get; set; }
}

public class ReviewDto
{
  public string Id { get; set; } = default!;
  public int Rating { get; set; }
  public string? Comment { get; set; }
  public DateTime CreatedAt { get; set; }
}
