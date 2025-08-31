using System.ComponentModel.DataAnnotations;

namespace Api.Contracts.Auth;

public class RegisterDto
{
    [Required, StringLength(256)]
    public string Email { get; set; } = default!;

    [Required, MinLength(6)]
    public string Password { get; set; } = default!;

    // Admin/Company/Driver/Rider
    [Required]
    public string Role { get; set; } = default!;
}
