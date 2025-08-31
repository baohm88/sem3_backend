using System.ComponentModel.DataAnnotations;

namespace Api.Contracts.Drivers;

public class UpdateDriverDto
{
    /// <example>Nguyen Van A</example>
    public string? FullName { get; set; }

    /// <example>+84 912 345 678</example>
    public string? Phone { get; set; }

    /// <example>5-year experience, safe driving</example>
    public string? Bio { get; set; }

    /// <example>bike,city,english</example>
    public string? Skills { get; set; }

    /// <example>Ho Chi Minh</example>
    public string? Location { get; set; }

    /// <example>https://cdn.example.com/avatar.png</example>
    [Url]
    public string? ImgUrl { get; set; }

    /// <example>true</example>
    public bool? IsAvailable { get; set; }
}
