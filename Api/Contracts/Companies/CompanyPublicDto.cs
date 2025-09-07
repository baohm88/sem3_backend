using Domain.Entities; 

namespace Api.Contracts.Companies;

public class CompanyPublicDto
{
  public Company Company { get; set; } = default!;
  public decimal Rating { get; set; }
  public int ActiveServicesCount { get; set; }
  public int DriversCount { get; set; }
  public List<Service> Services { get; set; } = new();

  // optional: metadata để FE có thể phân trang khi cần
  public int Page { get; set; }
  public int Size { get; set; }
  public int TotalPages { get; set; }
  public int TotalItems { get; set; }
}

