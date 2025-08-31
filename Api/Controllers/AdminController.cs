using Api.Common;
using Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Api.Controllers;

[ApiController]
[Route("api/admin")]
[SwaggerTag("Admin")]
public class AdminController : ControllerBase
{
    private readonly AppDbContext _db;
    public AdminController(AppDbContext db) => _db = db;

    [HttpPost("users/{userId}/deactivate")]
    public async Task<ActionResult<ApiResponse<object>>> Deactivate(string userId, [FromBody] DeactivateDto dto)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user == null) return ApiResponse<object>.Fail("NOT_FOUND","User not found");
        user.IsActive = false;
        await _db.SaveChangesAsync();
        return ApiResponse<object>.Ok(new { user.Id, user.IsActive, dto.ReasonCode });
    }

    public record DeactivateDto(string ReasonCode, string? ReasonNote, DateTime? ExpiresAt);
}