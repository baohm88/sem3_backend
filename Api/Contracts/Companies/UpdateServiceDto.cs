using System.ComponentModel.DataAnnotations;

public class UpdateServiceDto
{
  /// <example>Premium Airport Pickup</example>
  public string? Title { get; set; }
  [Url]
  public string? ImgUrl { get; set; }

  /// <example>Comfort sedan with experienced driver</example>
  public string? Description { get; set; }

  /// <example>199000</example>
  public int? PriceCents { get; set; }

  /// <example>true</example>
  public bool? IsActive { get; set; }
}