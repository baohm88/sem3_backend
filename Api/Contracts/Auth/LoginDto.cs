using System.ComponentModel.DataAnnotations;

namespace Api.Contracts.Auth;

public class LoginDto
{
    [Required, StringLength(256)]
    public string Email { get; set; } = default!;
    [Required, MinLength(6)]
    public string Password { get; set; } = default!;
}
