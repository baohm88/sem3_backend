
using Api.Common;
using Api.Contracts.Drivers;
using Domain.Entities;
using Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;

namespace Api.Controllers;

[ApiController]
[Route("api/drivers")]
[SwaggerTag("Drivers")]
public class DriversController : ControllerBase
{
    private readonly AppDbContext _db;
    public DriversController(AppDbContext db) => _db = db;

    // ===== Helpers =====
    private string? GetUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier) ??
        User.FindFirstValue(ClaimTypes.Name) ??
        User.FindFirstValue("sub");

    private ActionResult<ApiResponse<T>> Forbidden<T>() =>
        ApiResponse<T>.Fail("FORBIDDEN", "Bạn không có quyền trên tài nguyên này");

    private async Task<DriverProfile?> GetOwnedDriverAsync(string driverUserId)
    {
        var uid = GetUserId();
        var d = await _db.DriverProfiles.FirstOrDefaultAsync(x => x.UserId == driverUserId);
        if (d == null) return null;
        return uid == driverUserId ? d : null;
    }

    // ========= Me =========
    [Authorize(Roles = "Driver")]
    [HttpGet("me")]
    [SwaggerOperation(Summary = "Get My Driver Profile")]
    [ProducesResponseType(typeof(ApiResponse<DriverProfile>), 200)]
    public async Task<ActionResult<ApiResponse<DriverProfile>>> GetMyProfile()
    {
        var uid = GetUserId();
        if (uid == null) return ApiResponse<DriverProfile>.Fail("UNAUTHORIZED", "Missing user id");
        var p = await _db.DriverProfiles.FirstOrDefaultAsync(x => x.UserId == uid);
        if (p == null)
        {
            p = new DriverProfile
            {
                Id = Guid.NewGuid().ToString("N")[..24],
                UserId = uid,
                FullName = "New Driver",
                Rating = 0,
                IsAvailable = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _db.DriverProfiles.Add(p);
            await _db.SaveChangesAsync();
        }
        return ApiResponse<DriverProfile>.Ok(p);
    }

    [Authorize(Roles = "Driver")]
    [HttpPut("me")]
    [SwaggerOperation(Summary = "Update My Driver Profile")]
    [ProducesResponseType(typeof(ApiResponse<DriverProfile>), 200)]
    public async Task<ActionResult<ApiResponse<DriverProfile>>> UpdateMyProfile([FromBody] UpdateDriverDto dto)
    {
        var uid = GetUserId();
        if (uid == null) return ApiResponse<DriverProfile>.Fail("UNAUTHORIZED", "Missing user id");
        var p = await _db.DriverProfiles.FirstOrDefaultAsync(x => x.UserId == uid);
        if (p == null) return ApiResponse<DriverProfile>.Fail("NOT_FOUND", "Profile không tồn tại");

        if (!string.IsNullOrWhiteSpace(dto.FullName)) p.FullName = dto.FullName!;
        if (dto.Phone is not null) p.Phone = dto.Phone;
        if (dto.Bio is not null) p.Bio = dto.Bio;
        if (dto.Skills is not null) p.Skills = dto.Skills;
        if (dto.Location is not null) p.Location = dto.Location;
        if (dto.ImgUrl is not null) p.ImgUrl = dto.ImgUrl;
        if (dto.IsAvailable.HasValue) p.IsAvailable = dto.IsAvailable.Value;
        p.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return ApiResponse<DriverProfile>.Ok(p);
    }

    // ========= Public listing & detail =========
    [HttpGet]
    [SwaggerOperation(Summary = "List Drivers")]
    [ProducesResponseType(typeof(ApiResponse<PageResult<DriverProfile>>), 200)]
    public async Task<ActionResult<ApiResponse<PageResult<DriverProfile>>>> ListDrivers(
        [FromQuery] string? name = null,
        [FromQuery] string? skills = null,
        [FromQuery] string? location = null,
        [FromQuery] bool? isAvailable = null,
        [FromQuery] decimal? minRating = null,
        [FromQuery] int page = 1, [FromQuery] int size = 10, [FromQuery] string? sort = "rating:desc")
    {
        var q = _db.DriverProfiles.AsQueryable();
        if (!string.IsNullOrWhiteSpace(name)) q = q.Where(d => d.FullName.Contains(name));
        if (!string.IsNullOrWhiteSpace(skills))
        {
            var parts = skills.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var p in parts) q = q.Where(d => d.Skills != null && d.Skills.Contains(p));
        }
        if (!string.IsNullOrWhiteSpace(location)) q = q.Where(d => d.Location == location);
        if (isAvailable.HasValue) q = q.Where(d => d.IsAvailable == isAvailable.Value);
        if (minRating.HasValue) q = q.Where(d => d.Rating >= minRating.Value);

        if (!string.IsNullOrWhiteSpace(sort))
        {
            var s = sort.Split(':'); var field = s[0]; var dir = s.Length > 1 ? s[1] : "asc";
            q = (field, dir) switch
            {
                ("name", "asc") => q.OrderBy(d => d.FullName),
                ("name", "desc") => q.OrderByDescending(d => d.FullName),
                ("rating", "asc") => q.OrderBy(d => d.Rating),
                ("rating", "desc") => q.OrderByDescending(d => d.Rating),
                _ => q.OrderByDescending(d => d.Rating)
            };
        }

        var total = await q.CountAsync();
        var items = await q.Skip((page - 1) * size).Take(size).ToListAsync();
        return ApiResponse<PageResult<DriverProfile>>.Ok(new PageResult<DriverProfile>
        {
            page = page, size = size, totalItems = total,
            totalPages = (int)Math.Ceiling(total / (double)size),
            hasNext = page * size < total, hasPrev = page > 1, items = items
        });
    }

    [HttpGet("{userId}")]
    [SwaggerOperation(Summary = "Get Driver by UserId")]
    [ProducesResponseType(typeof(ApiResponse<DriverProfile>), 200)]
    public async Task<ActionResult<ApiResponse<DriverProfile>>> GetDriverByUserId([FromRoute] string userId)
    {
        var p = await _db.DriverProfiles.FirstOrDefaultAsync(x => x.UserId == userId);
        if (p == null) return ApiResponse<DriverProfile>.Fail("NOT_FOUND", "Driver không tồn tại");
        return ApiResponse<DriverProfile>.Ok(p);
    }

    // ========= Availability =========
    [Authorize(Roles = "Driver")]
    [HttpPost("{userId}/availability")]
    [SwaggerOperation(Summary = "Set Availability")]
    [ProducesResponseType(typeof(ApiResponse<DriverProfile>), 200)]
    public async Task<ActionResult<ApiResponse<DriverProfile>>> SetAvailability([FromRoute] string userId, [FromBody] SetAvailabilityDto dto)
    {
        var p = await GetOwnedDriverAsync(userId);
        if (p == null) return Forbidden<DriverProfile>();
        p.IsAvailable = dto.IsAvailable;
        p.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return ApiResponse<DriverProfile>.Ok(p);
    }

    // ========= Wallet =========
    [Authorize(Roles = "Driver")]
    [HttpGet("{userId}/wallet")]
    [SwaggerOperation(Summary = "Get Driver Wallet")]
    [ProducesResponseType(typeof(ApiResponse<Wallet>), 200)]
    public async Task<ActionResult<ApiResponse<Wallet>>> GetWallet([FromRoute] string userId)
    {
        var p = await GetOwnedDriverAsync(userId);
        if (p == null) return Forbidden<Wallet>();

        var wallet = await _db.Wallets.FirstOrDefaultAsync(w => w.OwnerType == "Driver" && w.OwnerRefId == userId);
        if (wallet == null)
        {
            wallet = new Wallet
            {
                Id = Guid.NewGuid().ToString("N")[..24],
                OwnerType = "Driver",
                OwnerRefId = userId,
                BalanceCents = 0,
                LowBalanceThreshold = 10000,
                UpdatedAt = DateTime.UtcNow
            };
            _db.Wallets.Add(wallet);
            await _db.SaveChangesAsync();
        }
        return ApiResponse<Wallet>.Ok(wallet);
    }

    [Authorize(Roles = "Driver")]
    [HttpGet("{userId}/transactions")]
    [SwaggerOperation(Summary = "List Driver Transactions")]
    [ProducesResponseType(typeof(ApiResponse<PageResult<Transaction>>), 200)]
    public async Task<ActionResult<ApiResponse<PageResult<Transaction>>>> GetTransactions(
        [FromRoute] string userId, [FromQuery] int page = 1, [FromQuery] int size = 10)
    {
        var p = await GetOwnedDriverAsync(userId);
        if (p == null) return Forbidden<PageResult<Transaction>>();

        var wallet = await _db.Wallets.FirstOrDefaultAsync(w => w.OwnerType == "Driver" && w.OwnerRefId == userId);
        if (wallet == null)
        {
            return ApiResponse<PageResult<Transaction>>.Ok(new PageResult<Transaction>
            {
                page = page, size = size, totalItems = 0, totalPages = 0, hasNext = false, hasPrev = false, items = Array.Empty<Transaction>()
            });
        }

        var q = _db.Transactions.Where(t => t.FromWalletId == wallet.Id || t.ToWalletId == wallet.Id)
                                .OrderByDescending(t => t.CreatedAt);
        var total = await q.CountAsync();
        var items = await q.Skip((page - 1) * size).Take(size).ToListAsync();
        return ApiResponse<PageResult<Transaction>>.Ok(new PageResult<Transaction>
        {
            page = page, size = size, totalItems = total, totalPages = (int)Math.Ceiling(total / (double)size),
            hasNext = page * size < total, hasPrev = page > 1, items = items
        });
    }

    [Authorize(Roles = "Driver")]
    [HttpPost("{userId}/wallet/withdraw")]
    [SwaggerOperation(Summary = "Withdraw from Driver Wallet")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    public async Task<ActionResult<ApiResponse<object>>> Withdraw([FromRoute] string userId, [FromBody] WithdrawDto dto)
    {
        var p = await GetOwnedDriverAsync(userId);
        if (p == null) return Forbidden<object>();
        if (dto.AmountCents <= 0) return ApiResponse<object>.Fail("VALIDATION", "AmountCents phải > 0");

        var wallet = await _db.Wallets.FirstOrDefaultAsync(w => w.OwnerType == "Driver" && w.OwnerRefId == userId);
        if (wallet == null || wallet.BalanceCents < dto.AmountCents)
            return ApiResponse<object>.Fail("INSUFFICIENT_FUNDS", "Số dư không đủ");

        if (!string.IsNullOrWhiteSpace(dto.IdempotencyKey))
        {
            var exists = await _db.Transactions.AnyAsync(t => t.IdempotencyKey == dto.IdempotencyKey);
            if (exists) return ApiResponse<object>.Ok(new { balance = wallet.BalanceCents });
        }

        wallet.BalanceCents -= dto.AmountCents;
        wallet.UpdatedAt = DateTime.UtcNow;

        var tx = new Transaction
        {
            Id = Guid.NewGuid().ToString("N")[..24],
            FromWalletId = wallet.Id,
            ToWalletId = null,
            AmountCents = dto.AmountCents,
            Status = TxStatus.Completed,
            IdempotencyKey = dto.IdempotencyKey,
            CreatedAt = DateTime.UtcNow
        };
        _db.Transactions.Add(tx);
        await _db.SaveChangesAsync();

        return ApiResponse<object>.Ok(new { wallet.Id, balance = wallet.BalanceCents });
    }

    // ========= Relationships / Companies =========
    [HttpGet("{userId}/companies")]
    [SwaggerOperation(Summary = "List Companies of Driver")]
    [ProducesResponseType(typeof(ApiResponse<PageResult<Company>>), 200)]
    public async Task<ActionResult<ApiResponse<PageResult<Company>>>> ListMyCompanies(
        [FromRoute] string userId, [FromQuery] int page = 1, [FromQuery] int size = 10)
    {
        var q = from rel in _db.CompanyDriverRelations
                where rel.DriverUserId == userId
                join c in _db.Companies on rel.CompanyId equals c.Id
                orderby c.Name
                select c;

        var total = await q.CountAsync();
        var items = await q.Skip((page - 1) * size).Take(size).ToListAsync();
        return ApiResponse<PageResult<Company>>.Ok(new PageResult<Company>
        {
            page = page, size = size, totalItems = total, totalPages = (int)Math.Ceiling(total / (double)size),
            hasNext = page * size < total, hasPrev = page > 1, items = items
        });
    }

    // ========= Applications (apply to company) =========
    [Authorize(Roles = "Driver")]
    [HttpPost("{userId}/applications")]
    [SwaggerOperation(Summary = "Apply to Company")]
    [ProducesResponseType(typeof(ApiResponse<JobApplication>), 200)]
    public async Task<ActionResult<ApiResponse<JobApplication>>> ApplyToCompany([FromRoute] string userId, [FromBody] ApplyCompanyDto dto)
    {
        var p = await GetOwnedDriverAsync(userId);
        if (p == null) return Forbidden<JobApplication>();
        if (string.IsNullOrWhiteSpace(dto.CompanyId)) return ApiResponse<JobApplication>.Fail("VALIDATION", "CompanyId bắt buộc");

        var dup = await _db.JobApplications.AnyAsync(a => a.CompanyId == dto.CompanyId && a.DriverUserId == userId && a.Status == ApplyStatus.Applied);
        if (dup) return ApiResponse<JobApplication>.Fail("DUPLICATE", "Bạn đã ứng tuyển và đang chờ xử lý");

        var app = new JobApplication
        {
            Id = Guid.NewGuid().ToString("N")[..24],
            CompanyId = dto.CompanyId!,
            DriverUserId = userId,
            Status = ApplyStatus.Applied,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = dto.ExpiresAt
        };
        _db.JobApplications.Add(app);
        await _db.SaveChangesAsync();
        return ApiResponse<JobApplication>.Ok(app);
    }

    [Authorize(Roles = "Driver")]
    [HttpGet("{userId}/applications")]
    [SwaggerOperation(Summary = "List My Applications")]
    [ProducesResponseType(typeof(ApiResponse<PageResult<JobApplication>>), 200)]
    public async Task<ActionResult<ApiResponse<PageResult<JobApplication>>>> ListMyApplications(
        [FromRoute] string userId, [FromQuery] int page = 1, [FromQuery] int size = 10, [FromQuery] string? status = null)
    {
        var p = await GetOwnedDriverAsync(userId);
        if (p == null) return Forbidden<PageResult<JobApplication>>();

        var q = _db.JobApplications.Where(a => a.DriverUserId == userId);
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<ApplyStatus>(status, true, out var st))
            q = q.Where(a => a.Status == st);

        q = q.OrderByDescending(a => a.CreatedAt);
        var total = await q.CountAsync();
        var items = await q.Skip((page - 1) * size).Take(size).ToListAsync();
        return ApiResponse<PageResult<JobApplication>>.Ok(new PageResult<JobApplication>
        {
            page = page, size = size, totalItems = total, totalPages = (int)Math.Ceiling(total / (double)size),
            hasNext = page * size < total, hasPrev = page > 1, items = items
        });
    }

    // ========= Invitations =========
    [Authorize(Roles = "Driver")]
    [HttpGet("{userId}/invitations")]
    [SwaggerOperation(Summary = "List My Invitations")]
    [ProducesResponseType(typeof(ApiResponse<PageResult<Invite>>), 200)]
    public async Task<ActionResult<ApiResponse<PageResult<Invite>>>> ListMyInvitations(
        [FromRoute] string userId, [FromQuery] int page = 1, [FromQuery] int size = 10)
    {
        var p = await GetOwnedDriverAsync(userId);
        if (p == null) return Forbidden<PageResult<Invite>>();

        var q = _db.Invites.Where(i => i.DriverUserId == userId).OrderByDescending(i => i.CreatedAt);
        var total = await q.CountAsync();
        var items = await q.Skip((page - 1) * size).Take(size).ToListAsync();
        return ApiResponse<PageResult<Invite>>.Ok(new PageResult<Invite>
        {
            page = page, size = size, totalItems = total, totalPages = (int)Math.Ceiling(total / (double)size),
            hasNext = page * size < total, hasPrev = page > 1, items = items
        });
    }

    [Authorize(Roles = "Driver")]
    [HttpPost("{userId}/invitations/{inviteId}/accept")]
    [SwaggerOperation(Summary = "Accept Invitation")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    public async Task<ActionResult<ApiResponse<object>>> AcceptInvitation([FromRoute] string userId, [FromRoute] string inviteId)
    {
        var p = await GetOwnedDriverAsync(userId);
        if (p == null) return Forbidden<object>();

        var inv = await _db.Invites.FirstOrDefaultAsync(i => i.Id == inviteId && i.DriverUserId == userId);
        if (inv == null) return ApiResponse<object>.Fail("NOT_FOUND", "Invite không tồn tại");
        if (inv.Status != "Sent") return ApiResponse<object>.Fail("INVALID_STATE", "Invite đã được xử lý");

        inv.Status = "Accepted";

        var exists = await _db.CompanyDriverRelations.AnyAsync(r => r.CompanyId == inv.CompanyId && r.DriverUserId == userId);
        if (!exists)
        {
            _db.CompanyDriverRelations.Add(new CompanyDriverRelation
            {
                Id = Guid.NewGuid().ToString("N")[..24],
                CompanyId = inv.CompanyId,
                DriverUserId = userId,
                BaseSalaryCents = inv.BaseSalaryCents,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        await _db.SaveChangesAsync();
        return ApiResponse<object>.Ok(new { inviteId = inv.Id, status = inv.Status });
    }

    [Authorize(Roles = "Driver")]
    [HttpPost("{userId}/invitations/{inviteId}/reject")]
    [SwaggerOperation(Summary = "Reject Invitation")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    public async Task<ActionResult<ApiResponse<object>>> RejectInvitation([FromRoute] string userId, [FromRoute] string inviteId)
    {
        var p = await GetOwnedDriverAsync(userId);
        if (p == null) return Forbidden<object>();

        var inv = await _db.Invites.FirstOrDefaultAsync(i => i.Id == inviteId && i.DriverUserId == userId);
        if (inv == null) return ApiResponse<object>.Fail("NOT_FOUND", "Invite không tồn tại");
        if (inv.Status != "Sent") return ApiResponse<object>.Fail("INVALID_STATE", "Invite đã được xử lý");

        inv.Status = "Rejected";
        await _db.SaveChangesAsync();
        return ApiResponse<object>.Ok(new { inviteId = inv.Id, status = inv.Status });
    }
}
