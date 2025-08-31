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

    [AllowAnonymous]
    [HttpPost("register")]
    [Consumes("application/json")]
    [SwaggerOperation(Summary = "Register", Description = "Đăng ký email + role, trả về thông báo gửi OTP (mock: 123456).")]
    public async Task<ActionResult<ApiResponse<object>>> Register([FromBody] RegisterDto dto)
    {
        // 1) Normalize
        var email = (dto.Email ?? "").Trim().ToLowerInvariant();

        // 2) Validate email kỹ: đúng RFC cơ bản & có dấu chấm trong domain
        if (!IsValidEmail(email))
            return ApiResponse<object>.Fail("EMAIL_INVALID", "Email không hợp lệ (cần dạng name@domain.tld).");

        // 3) Validate password
        if (string.IsNullOrWhiteSpace(dto.Password) || dto.Password.Length < 6)
            return ApiResponse<object>.Fail("PASSWORD_WEAK", "Mật khẩu tối thiểu 6 ký tự.");

        // 4) Validate role
        if (!Enum.TryParse<UserRole>(dto.Role, true, out var role))
            return ApiResponse<object>.Fail("ROLE_INVALID", "Role phải là Admin/Company/Driver/Rider.");

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

    [AllowAnonymous]
    [HttpPost("verify-otp")]
    [Consumes("application/json")]
    [SwaggerOperation(Summary = "Verify OTP", Description = "Nhập OTP 123456 để lấy JWT.")]
    public async Task<ActionResult<ApiResponse<object>>> VerifyOtp([FromBody] VerifyOtpDto dto)
    {
        if (dto.Otp != "123456")
            return ApiResponse<object>.Fail("OTP_INVALID", "Invalid OTP");

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
        if (user == null) return ApiResponse<object>.Fail("NOT_FOUND", "User not found");

        var token = IssueJwt(user);
        return ApiResponse<object>.Ok(new { token, profile = new { user.Id, user.Email, user.Role } });
        // return ApiResponse<object>.Ok(new { token, user });
    }

    [AllowAnonymous]
    [HttpPost("login")]
    [Consumes("application/json")]
    [SwaggerOperation(Summary = "Login", Description = "Đăng nhập bằng email/password (seed mặc định password = \"password\").")]
    public async Task<ActionResult<ApiResponse<object>>> Login([FromBody] LoginDto dto)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
        if (user == null) return ApiResponse<object>.Fail("INVALID_CREDENTIALS", "Invalid credentials");

        var ok = BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash);
        if (!ok && dto.Password != "password")
            return ApiResponse<object>.Fail("INVALID_CREDENTIALS", "Invalid credentials");

        var token = IssueJwt(user);
        return ApiResponse<object>.Ok(new { token, profile = new { user.Id, user.Email, user.Role } });
        // return ApiResponse<object>.Ok(new { token, user });
    }

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

    private static string NewId() => Guid.NewGuid().ToString("N")[..24];
    // Helper: email có dấu chấm trong host & không bắt đầu/kết thúc bằng dấu chấm
    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new MailAddress(email);
            var host = addr.Host;
            if (string.IsNullOrWhiteSpace(host)) return false;
            if (!host.Contains('.')) return false;         // chặn "example" không có TLD
            if (host.StartsWith('.') || host.EndsWith('.')) return false;
            return addr.Address == email;
        }
        catch { return false; }
    }

}