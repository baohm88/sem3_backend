using System.ComponentModel.DataAnnotations;

namespace Api.Contracts.Riders;

/// <summary>
/// Payload for updating a Rider's public profile fields.
/// All properties are optional; only provided fields will be updated.
/// </summary>
public class UpdateRiderDto
{
    /// <summary>Rider's full name.</summary>
    public string? FullName { get; set; }

    /// <summary>Phone number.</summary>
    public string? Phone { get; set; }

    /// <summary>Avatar image URL.</summary>
    [Url]
    public string? ImgUrl { get; set; }
}
