using Api.Common;
using Api.Contracts.Drivers;
using Domain.Entities;
using Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;
using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Api.Controllers;

[ApiController]
[Route("api/drivers")]
[SwaggerTag("Drivers")]
public class DriversController : ControllerBase
{
  private readonly AppDbContext _db;
  public DriversController(AppDbContext db) => _db = db;

  // ===== Helpers =====

  /// <summary>Resolve current user id from claims.</summary>
  private string? GetUserId() =>
      User.FindFirstValue(ClaimTypes.NameIdentifier) ??
      User.FindFirstValue(ClaimTypes.Name) ??
      User.FindFirstValue("sub");

  /// <summary>Convenience Forbidden response.</summary>
  private ActionResult<ApiResponse<T>> Forbidden<T>() =>
      ApiResponse<T>.Fail("FORBIDDEN", "You don't have permission on this resource.");

  /// <summary>Get driver profile if it belongs to the current user; otherwise null.</summary>
  private async Task<DriverProfile?> GetOwnedDriverAsync(string driverUserId)
  {
    var uid = GetUserId();
    var d = await _db.DriverProfiles.FirstOrDefaultAsync(x => x.UserId == driverUserId);
    if (d == null) return null;
    return uid == driverUserId ? d : null;
  }

  private static string NewId() => Guid.NewGuid().ToString("N")[..24];

  /// <summary>
  /// Normalize skills input: accept JSON array or CSV; trim, deduplicate (case-insensitive), re-serialize as JSON array.
  /// </summary>
  private static string? NormalizeSkills(string? input)
  {
    if (string.IsNullOrWhiteSpace(input)) return null;
    var s = input.Trim();

    // 1) If it's already a valid JSON array -> parse & re-serialize cleanly
    if (s.StartsWith("["))
    {
      try
      {
        var arr = JsonSerializer.Deserialize<List<string>>(s);
        if (arr != null)
        {
          var clean = arr
              .Where(x => !string.IsNullOrWhiteSpace(x))
              .Select(x => x.Trim())
              .Distinct(StringComparer.OrdinalIgnoreCase)
              .ToList();
          return JsonSerializer.Serialize(clean);
        }
      }
      catch { /* fall through to CSV parse */ }
    }

    // 2) CSV -> array
    var parts = s.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                 .Select(x => x.Trim())
                 .Where(x => x.Length > 0)
                 .Distinct(StringComparer.OrdinalIgnoreCase)
                 .ToList();

    // If user typed a single token without commas, still accept it
    if (parts.Count == 0 && s.Length > 0) parts = new List<string> { s };

    return JsonSerializer.Serialize(parts);
  }

  // ========= Me =========

  [Authorize(Roles = "Driver")]
  [HttpGet("me")]
  [SwaggerOperation(Summary = "Get My Driver Profile (auto-create if missing)")]
  [ProducesResponseType(typeof(ApiResponse<DriverProfile>), 200)]
  public async Task<ActionResult<ApiResponse<DriverProfile>>> GetMyDriver()
  {
    var uid = GetUserId();
    if (uid == null) return ApiResponse<DriverProfile>.Fail("UNAUTHORIZED", "Missing user id");

    var p = await _db.DriverProfiles.FirstOrDefaultAsync(x => x.UserId == uid);
    if (p == null)
    {
      var myEmail = await _db.Users.Where(u => u.Id == uid).Select(u => u.Email).FirstAsync();
      p = new DriverProfile
      {
        Id = NewId(),
        UserId = uid,
        FullName = "New Driver",
        Email = myEmail,
        Phone = null,
        Bio = null,
        Rating = 0,
        Skills = null,
        Location = null,
        IsAvailable = true,
        ImgUrl = null,
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
  [Consumes("application/json")]
  [SwaggerOperation(Summary = "Update My Driver Profile (upsert)")]
  [ProducesResponseType(typeof(ApiResponse<DriverProfile>), 200)]
  public async Task<ActionResult<ApiResponse<DriverProfile>>> UpdateMyDriver([FromBody] UpdateDriverDto dto)
  {
    var uid = GetUserId();
    if (uid == null) return ApiResponse<DriverProfile>.Fail("UNAUTHORIZED", "Missing user id");

    var p = await _db.DriverProfiles.FirstOrDefaultAsync(x => x.UserId == uid);
    if (p == null)
    {
      var myEmail = await _db.Users.Where(u => u.Id == uid).Select(u => u.Email).FirstAsync();
      p = new DriverProfile
      {
        Id = NewId(),
        UserId = uid,
        FullName = dto.FullName ?? "New Driver",
        Email = myEmail,
        Phone = dto.Phone,
        Bio = null,
        Rating = 0,
        Skills = NormalizeSkills(dto.Skills),
        Location = dto.Location,
        IsAvailable = true,
        ImgUrl = UrlHelper.TryNormalizeUrl(dto.ImgUrl),
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
      };
      _db.DriverProfiles.Add(p);
    }
    else
    {
      if (dto.FullName is not null) p.FullName = dto.FullName;
      if (dto.Phone is not null) p.Phone = dto.Phone;
      if (dto.Bio is not null) p.Bio = dto.Bio;
      if (dto.Skills is not null) p.Skills = NormalizeSkills(dto.Skills);

      if (dto.Location is not null) p.Location = dto.Location;
      if (dto.ImgUrl is not null)
      {
        var normalized = UrlHelper.TryNormalizeUrl(dto.ImgUrl);
        if (normalized == null)
          return ApiResponse<DriverProfile>.Fail("IMG_URL_INVALID", "Avatar is not a valid URL (http/https).");
        p.ImgUrl = normalized;
      }

      if (dto.IsAvailable is not null) p.IsAvailable = dto.IsAvailable.Value;

      p.UpdatedAt = DateTime.UtcNow;
    }

    await _db.SaveChangesAsync();
    return ApiResponse<DriverProfile>.Ok(p);
  }

  // ========= Public listing & detail =========

  [HttpGet]
  [SwaggerOperation(Summary = "List Drivers")]
  [ProducesResponseType(typeof(ApiResponse<PageResult<DriverForListDto>>), 200)]
  public async Task<ActionResult<ApiResponse<PageResult<DriverForListDto>>>> ListDrivers(
    [FromQuery] string? name = null,
    [FromQuery] string? skills = null,
    [FromQuery] string? location = null,
    [FromQuery] bool? isAvailable = null,
    [FromQuery] decimal? minRating = null,
    // NEW: allow hiding drivers who are already hired
    [FromQuery] bool? excludeHired = null,
    [FromQuery] int page = 1,
    [FromQuery] int size = 10,
    [FromQuery] string? sort = "rating:desc")
  {
    var q = _db.DriverProfiles.AsQueryable();

    if (!string.IsNullOrWhiteSpace(name))
      q = q.Where(d => d.FullName.Contains(name));

    if (!string.IsNullOrWhiteSpace(skills))
    {
      var parts = skills.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
      foreach (var p in parts)
        q = q.Where(d => d.Skills != null && d.Skills.Contains(p));
    }

    if (!string.IsNullOrWhiteSpace(location))
      q = q.Where(d => d.Location == location);

    if (isAvailable.HasValue)
      q = q.Where(d => d.IsAvailable == isAvailable.Value);

    if (minRating.HasValue)
      q = q.Where(d => d.Rating >= minRating.Value);

    // NEW: filter out drivers that have any relation with a company
    if (excludeHired == true)
    {
      q = q.Where(d => !_db.CompanyDriverRelations
                        .Any(r => r.DriverUserId == d.UserId));
    }

    // sort (preserve existing logic)
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

    // Project to DTO and compute IsHired using a subquery Any()
    var items = await q
      .Skip((page - 1) * size)
      .Take(size)
      .Select(d => new DriverForListDto
      {
        Id = d.Id,
        UserId = d.UserId,
        FullName = d.FullName,
        Email = d.Email,
        Phone = d.Phone,
        Bio = d.Bio,
        ImgUrl = d.ImgUrl,
        Rating = d.Rating,
        Skills = d.Skills,
        Location = d.Location,
        IsAvailable = d.IsAvailable,
        CreatedAt = d.CreatedAt,
        UpdatedAt = d.UpdatedAt,
        IsHired = _db.CompanyDriverRelations.Any(r => r.DriverUserId == d.UserId)
      })
      .ToListAsync();

    return ApiResponse<PageResult<DriverForListDto>>.Ok(new PageResult<DriverForListDto>
    {
      page = page,
      size = size,
      totalItems = total,
      totalPages = (int)Math.Ceiling(total / (double)size),
      hasNext = page * size < total,
      hasPrev = page > 1,
      items = items
    });
  }

  [HttpGet("{userId}")]
  [SwaggerOperation(Summary = "Get Driver by UserId")]
  [ProducesResponseType(typeof(ApiResponse<DriverProfile>), 200)]
  public async Task<ActionResult<ApiResponse<DriverProfile>>> GetDriverByUserId([FromRoute] string userId)
  {
    var p = await _db.DriverProfiles.FirstOrDefaultAsync(x => x.UserId == userId);
    if (p == null) return ApiResponse<DriverProfile>.Fail("NOT_FOUND", "Driver does not exist.");
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
        Id = NewId(),
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
  [HttpPost("{userId}/wallet/topup")]
  [SwaggerOperation(Summary = "Topup Driver Wallet")]
  [ProducesResponseType(typeof(ApiResponse<object>), 200)]
  public async Task<ActionResult<ApiResponse<object>>> TopupDriverWallet(
    [FromRoute] string userId,
    [FromBody] Api.Contracts.Drivers.TopupDto dto)
  {
    // Only allow the owner to top up their own wallet
    var uid = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
    if (uid == null || uid != userId)
      return ApiResponse<object>.Fail("FORBIDDEN", "You do not have permission to top up this user's wallet.");

    if (dto.AmountCents <= 0)
      return ApiResponse<object>.Fail("VALIDATION", "AmountCents must be > 0.");

    // Get (or create) driver wallet
    var wallet = await _db.Wallets.FirstOrDefaultAsync(
        w => w.OwnerType == "Driver" && w.OwnerRefId == userId);
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
    }

    // Idempotency
    if (!string.IsNullOrWhiteSpace(dto.IdempotencyKey))
    {
      var exists = await _db.Transactions.AnyAsync(t => t.IdempotencyKey == dto.IdempotencyKey);
      if (exists) return ApiResponse<object>.Ok(new { wallet.Id, balance = wallet.BalanceCents });
    }

    // Add funds
    wallet.BalanceCents += dto.AmountCents;
    wallet.UpdatedAt = DateTime.UtcNow;

    // Record transaction
    var tx = new Transaction
    {
      Id = Guid.NewGuid().ToString("N")[..24],
      FromWalletId = null,                 // topup: funds enter system from outside
      ToWalletId = wallet.Id,
      AmountCents = dto.AmountCents,
      Status = TxStatus.Completed,
      IdempotencyKey = dto.IdempotencyKey,
      CreatedAt = DateTime.UtcNow,
      Type = TxType.Topup,
      RefId = userId,
      MetaJson = JsonSerializer.Serialize(new { driverUserId = userId, source = "manual" })
    };
    _db.Transactions.Add(tx);

    await _db.SaveChangesAsync();

    return ApiResponse<object>.Ok(new { wallet.Id, balance = wallet.BalanceCents, transactionId = tx.Id });
  }

  [Authorize(Roles = "Driver")]
  [HttpPost("{userId}/wallet/withdraw")]
  public async Task<ActionResult<ApiResponse<object>>> Withdraw([FromRoute] string userId, [FromBody] WithdrawDto dto)
  {
    var p = await GetOwnedDriverAsync(userId);
    if (p == null) return Forbidden<object>();
    if (dto.AmountCents <= 0) return ApiResponse<object>.Fail("VALIDATION", "AmountCents must be > 0.");

    var wallet = await _db.Wallets.FirstOrDefaultAsync(w => w.OwnerType == "Driver" && w.OwnerRefId == userId);
    if (wallet == null) return ApiResponse<object>.Fail("NO_WALLET", "Driver does not have a wallet."); // (optional: clearer message)
    if (wallet.BalanceCents < dto.AmountCents) return ApiResponse<object>.Fail("INSUFFICIENT_FUNDS", "Insufficient balance.");

    if (!string.IsNullOrWhiteSpace(dto.IdempotencyKey))
    {
      var exists = await _db.Transactions.AnyAsync(t => t.IdempotencyKey == dto.IdempotencyKey);
      if (exists) return ApiResponse<object>.Ok(new { balance = wallet.BalanceCents });
    }

    wallet.BalanceCents -= dto.AmountCents;
    wallet.UpdatedAt = DateTime.UtcNow;

    var tx = new Transaction
    {
      Id = NewId(),
      FromWalletId = wallet.Id,
      ToWalletId = null,
      AmountCents = dto.AmountCents,
      Status = TxStatus.Completed,
      IdempotencyKey = dto.IdempotencyKey,
      CreatedAt = DateTime.UtcNow,
      Type = TxType.Withdraw,                                      // IMPORTANT
      RefId = userId,                                              // (recommended)
      MetaJson = JsonSerializer.Serialize(new { driverUserId = userId, method = "manual" }) // (recommended)
    };

    _db.Transactions.Add(tx);
    await _db.SaveChangesAsync();

    return ApiResponse<object>.Ok(new { wallet.Id, balance = wallet.BalanceCents, transactionId = tx.Id }); // (recommended)
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
        page = page,
        size = size,
        totalItems = 0,
        totalPages = 0,
        hasNext = false,
        hasPrev = false,
        items = Array.Empty<Transaction>()
      });
    }

    var q = _db.Transactions.Where(t => t.FromWalletId == wallet.Id || t.ToWalletId == wallet.Id)
                            .OrderByDescending(t => t.CreatedAt);
    var total = await q.CountAsync();
    var items = await q.Skip((page - 1) * size).Take(size).ToListAsync();
    return ApiResponse<PageResult<Transaction>>.Ok(new PageResult<Transaction>
    {
      page = page,
      size = size,
      totalItems = total,
      totalPages = (int)Math.Ceiling(total / (double)size),
      hasNext = page * size < total,
      hasPrev = page > 1,
      items = items
    });
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
      page = page,
      size = size,
      totalItems = total,
      totalPages = (int)Math.Ceiling(total / (double)size),
      hasNext = page * size < total,
      hasPrev = page > 1,
      items = items
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
    if (string.IsNullOrWhiteSpace(dto.CompanyId)) return ApiResponse<JobApplication>.Fail("VALIDATION", "CompanyId is required.");

    var dup = await _db.JobApplications.AnyAsync(a => a.CompanyId == dto.CompanyId && a.DriverUserId == userId && a.Status == ApplyStatus.Applied);
    if (dup) return ApiResponse<JobApplication>.Fail("DUPLICATE", "You have already applied and are pending.");

    var alreadyHired = await _db.CompanyDriverRelations.AnyAsync(r => r.DriverUserId == userId);
    if (alreadyHired) return ApiResponse<JobApplication>.Fail("ALREADY_EMPLOYED", "You are already employed by a company and cannot apply elsewhere.");

    var app = new JobApplication
    {
      Id = NewId(),
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
      page = page,
      size = size,
      totalItems = total,
      totalPages = (int)Math.Ceiling(total / (double)size),
      hasNext = page * size < total,
      hasPrev = page > 1,
      items = items
    });
  }

  [Authorize(Roles = "Driver")]
  [HttpDelete("{userId}/applications/{applicationId}")]
  [SwaggerOperation(Summary = "Cancel/Recall Job Application")]
  [ProducesResponseType(typeof(ApiResponse<object>), 200)]
  public async Task<ActionResult<ApiResponse<object>>> CancelApplication(
  [FromRoute] string userId,
  [FromRoute] string applicationId)
  {
    var p = await GetOwnedDriverAsync(userId);
    if (p == null) return Forbidden<object>();

    var app = await _db.JobApplications
        .FirstOrDefaultAsync(a => a.Id == applicationId && a.DriverUserId == userId);

    if (app == null)
      return ApiResponse<object>.Fail("NOT_FOUND", "Application does not exist.");

    if (app.Status != ApplyStatus.Applied)
      return ApiResponse<object>.Fail("INVALID_STATE", "Cannot cancel this application.");

    app.Status = ApplyStatus.Cancelled;
    await _db.SaveChangesAsync();

    return ApiResponse<object>.Ok(new { applicationId = app.Id, status = app.Status });
  }

  // ========= Invitations =========

  [Authorize(Roles = "Driver")]
  [HttpGet("{userId}/invitations")]
  [SwaggerOperation(Summary = "List My Invitations")]
  [ProducesResponseType(typeof(ApiResponse<PageResult<Invite>>), 200)]
  public async Task<ActionResult<ApiResponse<PageResult<Invite>>>> ListMyInvitations(
      [FromRoute] string userId, [FromQuery] int page = 1, [FromQuery] int size = 10, [FromQuery] string? status = null)
  {
    var p = await GetOwnedDriverAsync(userId);
    if (p == null) return Forbidden<PageResult<Invite>>();

    var q = _db.Invites.Where(i => i.DriverUserId == userId);

    if (!string.IsNullOrWhiteSpace(status))
    {
      q = q.Where(i => i.Status == status);
    }

    q = q.OrderByDescending(i => i.CreatedAt);

    var total = await q.CountAsync();
    var items = await q.Skip((page - 1) * size).Take(size).ToListAsync();
    return ApiResponse<PageResult<Invite>>.Ok(new PageResult<Invite>
    {
      page = page,
      size = size,
      totalItems = total,
      totalPages = (int)Math.Ceiling(total / (double)size),
      hasNext = page * size < total,
      hasPrev = page > 1,
      items = items
    });
  }

  [Authorize(Roles = "Driver")]
  [HttpPost("{userId}/invitations/{inviteId}/accept")]
  [SwaggerOperation(Summary = "Accept Invitation")]
  [ProducesResponseType(typeof(ApiResponse<object>), 200)]
  public async Task<ActionResult<ApiResponse<object>>> AcceptInvitation(
    [FromRoute] string userId,
    [FromRoute] string inviteId)
  {
    var p = await GetOwnedDriverAsync(userId);
    if (p == null) return Forbidden<object>();

    var inv = await _db.Invites
      .FirstOrDefaultAsync(i => i.Id == inviteId && i.DriverUserId == userId);
    if (inv == null) return ApiResponse<object>.Fail("NOT_FOUND", "Invite does not exist.");

    // Keep logic: only process when Pending
    if (!string.Equals(inv.Status, "Pending", StringComparison.OrdinalIgnoreCase))
      return ApiResponse<object>.Fail("INVALID_STATE", "Invite has already been processed.");

    // Transaction so all changes are committed together
    await using var tx = await _db.Database.BeginTransactionAsync();
    try
    {
      // 1) Accept the invite
      inv.Status = "Accepted";

      // 2) If relation company-driver doesn't exist, create it
      var exists = await _db.CompanyDriverRelations.AnyAsync(
        r => r.CompanyId == inv.CompanyId && r.DriverUserId == userId);
      if (!exists)
      {
        _db.CompanyDriverRelations.Add(new CompanyDriverRelation
        {
          Id = NewId(),
          CompanyId = inv.CompanyId,
          DriverUserId = userId,
          BaseSalaryCents = inv.BaseSalaryCents,
          CreatedAt = DateTime.UtcNow,
          UpdatedAt = DateTime.UtcNow
        });
      }

      // 3) CANCEL ALL JobApplications that are Applied for this driver (across all companies)
      var toCancel = await _db.JobApplications
        .Where(a => a.DriverUserId == userId && a.Status == ApplyStatus.Applied)
        .ToListAsync();

      foreach (var a in toCancel)
        a.Status = ApplyStatus.Cancelled;

      await _db.SaveChangesAsync();
      await tx.CommitAsync();

      return ApiResponse<object>.Ok(new { inviteId = inv.Id, status = inv.Status });
    }
    catch
    {
      await tx.RollbackAsync();
      throw;
    }
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
    if (inv == null) return ApiResponse<object>.Fail("NOT_FOUND", "Invite does not exist.");
    if (inv.Status != "Pending") return ApiResponse<object>.Fail("INVALID_STATE", "Invite has already been processed.");

    inv.Status = "Rejected";
    await _db.SaveChangesAsync();
    return ApiResponse<object>.Ok(new { inviteId = inv.Id, status = inv.Status });
  }

  // ========= check employment status =========

  [Authorize(Roles = "Driver")]
  [HttpGet("{userId}/employment")]
  [SwaggerOperation(Summary = "Employment status of driver")]
  [ProducesResponseType(typeof(ApiResponse<object>), 200)]
  public async Task<ActionResult<ApiResponse<object>>> GetEmploymentStatus([FromRoute] string userId)
  {
    var me = await GetOwnedDriverAsync(userId);
    if (me == null) return Forbidden<object>();

    var relation = await _db.CompanyDriverRelations.FirstOrDefaultAsync(r => r.DriverUserId == userId);

    return ApiResponse<object>.Ok(new
    {
      isHired = relation != null,
      companyId = relation?.CompanyId
    });
  }

  // PUBLIC API

  [AllowAnonymous]
  [HttpGet("{userId}/public")]
  [SwaggerOperation(Summary = "Get Driver Public Profile")]
  [ProducesResponseType(typeof(ApiResponse<DriverPublicProfileDto>), 200)]
  public async Task<ActionResult<ApiResponse<DriverPublicProfileDto>>> GetDriverPublicProfile([FromRoute] string userId)
  {
    // 1) Driver profile
    var driver = await _db.DriverProfiles.FirstOrDefaultAsync(d => d.UserId == userId);
    if (driver == null)
      return ApiResponse<DriverPublicProfileDto>.Fail("NOT_FOUND", "Driver does not exist.");

    // 2) Current hired company: take the latest relation
    var latestRel = await _db.CompanyDriverRelations
      .Where(r => r.DriverUserId == userId)
      .OrderByDescending(r => r.CreatedAt)
      .FirstOrDefaultAsync();

    CompanyBriefDto? hired = null;
    if (latestRel != null)
    {
      var comp = await _db.Companies.FirstOrDefaultAsync(c => c.Id == latestRel.CompanyId);
      if (comp != null)
      {
        hired = new CompanyBriefDto
        {
          Id = comp.Id,
          Name = comp.Name,
          ImgUrl = comp.ImgUrl,
          Membership = comp.Membership,
          Rating = comp.Rating
        };
      }
    }

    // 3) Employment history (simple: all relations)
    var history = await (from rel in _db.CompanyDriverRelations
                         join c in _db.Companies on rel.CompanyId equals c.Id
                         where rel.DriverUserId == userId
                         orderby rel.CreatedAt descending
                         select new EmploymentDto
                         {
                           CompanyId = c.Id,
                           CompanyName = c.Name,
                           CompanyImgUrl = c.ImgUrl,
                           Since = rel.CreatedAt,
                           BaseSalaryCents = rel.BaseSalaryCents
                         }).ToListAsync();

    // 4) Rating & Reviews: no linked source yet -> empty
    var dto = new DriverPublicProfileDto
    {
      Driver = driver,
      HiredCompany = hired,
      EmploymentHistory = history,
      Rating = 0,
      Reviews = new List<ReviewDto>()
    };

    return ApiResponse<DriverPublicProfileDto>.Ok(dto);
  }
}
