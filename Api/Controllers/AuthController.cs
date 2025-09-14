using Api.Common;
using Domain.Entities;
using Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
using Swashbuckle.AspNetCore.Annotations;
using Api.Contracts.Auth;
using Microsoft.AspNetCore.Authorization;
using System.Net.Mail;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[SwaggerTag("Auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;

    public AuthController(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    /// <summary>
    /// Register a user with email and role; responds with OTP sent notice (mocked).
    /// </summary>
    [AllowAnonymous]
    [HttpPost("register")]
    [Consumes("application/json")]
    [SwaggerOperation(
        Summary = "Register",
        Description = "Register with email + role; returns notice that OTP is sent (mock: 123456)."
    )]
    public async Task<ActionResult<ApiResponse<object>>> Register([FromBody] RegisterDto dto)
    {
        // 1) Normalize
        var email = (dto.Email ?? "").Trim().ToLowerInvariant();

        // 2) Strict email validation: basic RFC check & requires a dot in the domain
        if (!IsValidEmail(email))
            return ApiResponse<object>.Fail("EMAIL_INVALID", "Invalid email (expected name@domain.tld).");

        // 3) Validate password
        if (string.IsNullOrWhiteSpace(dto.Password) || dto.Password.Length < 6)
            return ApiResponse<object>.Fail("PASSWORD_WEAK", "Password must be at least 6 characters.");

        // 4) Validate role
        if (!Enum.TryParse<UserRole>(dto.Role, true, out var role))
            return ApiResponse<object>.Fail("ROLE_INVALID", "Role must be Admin/Company/Driver/Rider.");

        // 5) Unique email (case-insensitive)
        var exists = await _db.Users.AnyAsync(x => x.Email == email);
        if (exists)
            return ApiResponse<object>.Fail("EMAIL_TAKEN", "Email already exists");

        // 6) Create
        var user = new User
        {
            Id = NewId(),
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Role = role,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return ApiResponse<object>.Ok(new { message = "OTP sent to email (mock: 123456)" });
    }

    /// <summary>
    /// Verify the OTP (mock 123456) and receive a JWT.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("verify-otp")]
    [Consumes("application/json")]
    [SwaggerOperation(
        Summary = "Verify OTP",
        Description = "Enter OTP 123456 to obtain JWT."
    )]
    public async Task<ActionResult<ApiResponse<object>>> VerifyOtp([FromBody] VerifyOtpDto dto)
    {
        if (dto.Otp != "123456")
            return ApiResponse<object>.Fail("OTP_INVALID", "Invalid OTP");

        var email = (dto.Email ?? "").Trim().ToLowerInvariant();
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email);
        if (user == null) return ApiResponse<object>.Fail("NOT_FOUND", "User not found");

        if (!user.IsActive)
            return ApiResponse<object>.Fail("ACCOUNT_DISABLED", "This account is deactivated.");

        var token = IssueJwt(user);
        return ApiResponse<object>.Ok(new { token, profile = new { user.Id, user.Email, user.Role } });
    }

    /// <summary>
    /// Login with email/password (seed default password is "password").
    /// </summary>
    [AllowAnonymous]
    [HttpPost("login")]
    [Consumes("application/json")]
    [SwaggerOperation(
        Summary = "Login",
        Description = "Login with email/password (seed default password = \"password\")."
    )]
    public async Task<ActionResult<ApiResponse<object>>> Login([FromBody] LoginDto dto)
    {
        var email = (dto.Email ?? "").Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null)
            return ApiResponse<object>.Fail("INVALID_CREDENTIALS", "Invalid credentials");

        // optional: gate the "password" fallback to development only
        var devPwdBypass = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";

        var ok = BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash);
        if (!ok && !(devPwdBypass && dto.Password == "password"))
            return ApiResponse<object>.Fail("INVALID_CREDENTIALS", "Invalid credentials");

        if (!user.IsActive)
            return ApiResponse<object>.Fail("ACCOUNT_DISABLED", "This account is deactivated.");

        var token = IssueJwt(user);
        return ApiResponse<object>.Ok(new { token, profile = new { user.Id, user.Email, user.Role } });
    }

    /// <summary>
    /// Issues a signed JWT for the given user (7-day expiry).
    /// </summary>
    private string IssueJwt(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email)
        };
        var jwt = new JwtSecurityToken(
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }

    /// <summary>
    /// Generates a 24-char ID (hex-like) from a GUID (first 24 chars).
    /// </summary>
    private static string NewId() => Guid.NewGuid().ToString("N")[..24];

    /// <summary>
    /// Helper: email must contain a dot in the host and must not start/end with a dot.
    /// </summary>
    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new MailAddress(email);
            var host = addr.Host;
            if (string.IsNullOrWhiteSpace(host)) return false;
            if (!host.Contains('.')) return false;         // block hosts with no TLD like "example"
            if (host.StartsWith('.') || host.EndsWith('.')) return false;
            return addr.Address == email;
        }
        catch { return false; }
    }
}
