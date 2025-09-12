using Domain.Entities; 

namespace Api.Contracts.Companies;

/// <summary>
/// Public-facing DTO for company data exposed to clients.
/// </summary>
public class CompanyPublicDto
{
  /// <summary>
  /// Base company entity.
  /// </summary>
  public Company Company { get; set; } = default!;

  /// <summary>
  /// Aggregate rating (e.g., 0.0–5.0).
  /// </summary>
  public decimal Rating { get; set; }

  /// <summary>
  /// Number of currently active services provided by the company.
  /// </summary>
  public int ActiveServicesCount { get; set; }

  /// <summary>
  /// Number of drivers associated with the company.
  /// </summary>
  public int DriversCount { get; set; }

  /// <summary>
  /// Public list of services offered by the company.
  /// </summary>
  public List<Service> Services { get; set; } = new();

  // optional: pagination metadata so the FE can paginate when needed
  /// <summary>
  /// 1-based current page index (optional pagination metadata).
  /// </summary>
  public int Page { get; set; }

  /// <summary>
  /// Page size in items per page (optional pagination metadata).
  /// </summary>
  public int Size { get; set; }

  /// <summary>
  /// Total number of pages (optional pagination metadata).
  /// </summary>
  public int TotalPages { get; set; }

  /// <summary>
  /// Total number of items across all pages (optional pagination metadata).
  /// </summary>
  public int TotalItems { get; set; }
}
