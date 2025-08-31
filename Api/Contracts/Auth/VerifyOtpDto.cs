namespace Api.Contracts.Auth;

public class VerifyOtpDto
{
    public string Email { get; set; } = default!;
    public string Otp { get; set; } = default!;
}
