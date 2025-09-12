using System;

namespace Api.Contracts.Companies;

/// <summary>
/// Lightweight DTO describing a company's driver as exposed to clients.
/// </summary>
public class CompanyDriverDto
{
  /// <summary>
  /// Associated user identifier of the driver.
  /// </summary>
  public string UserId { get; set; } = default!;

  /// <summary>
  /// Driver's full display name.
  /// </summary>
  public string FullName { get; set; } = default!;

  /// <summary>
  /// Optional phone number (may be hidden for privacy).
  /// </summary>
  public string? Phone { get; set; }

  /// <summary>
  /// Short bio or introduction.
  /// </summary>
  public string? Bio { get; set; }

  /// <summary>
  /// Optional profile image URL.
  /// </summary>
  public string? ImgUrl { get; set; }

  /// <summary>
  /// Aggregate rating (e.g., 0.0–5.0).
  /// </summary>
  public decimal Rating { get; set; }

  /// <summary>
  /// Optional skills (e.g., serialized JSON or comma-separated tags).
  /// </summary>
  public string? Skills { get; set; }

  /// <summary>
  /// Optional textual location (city/area).
  /// </summary>
  public string? Location { get; set; }

  /// <summary>
  /// Whether the driver is currently available for jobs.
  /// </summary>
  public bool IsAvailable { get; set; }

  /// <summary>
  /// Timestamp when the driver joined the company (server time/UTC per system policy).
  /// </summary>
  public DateTime JoinedAt { get; set; }

  /// <summary>
  /// Base salary in cents (integer minor units).
  /// </summary>
  public int BaseSalaryCents { get; set; }
}
