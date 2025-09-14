// Api/Contracts/Drivers/DriverForListDto.cs
namespace Api.Contracts.Drivers;

public class DriverForListDto
{
  public string Id { get; set; } = default!;
  public string UserId { get; set; } = default!;
  public string FullName { get; set; } = default!;
  public string Email { get; set; } = default!;
  public string? Phone { get; set; }
  public string? Bio { get; set; }
  public string? ImgUrl { get; set; }
  public decimal Rating { get; set; }
  public string? Skills { get; set; }
  public string? Location { get; set; }
  public bool IsAvailable { get; set; }
  public DateTime CreatedAt { get; set; }
  public DateTime UpdatedAt { get; set; }
  public bool IsHired { get; set; }
}
