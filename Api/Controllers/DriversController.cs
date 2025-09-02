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

  private static string NewId() => Guid.NewGuid().ToString("N")[..24];

  private static string? NormalizeSkills(string? input)
  {
    if (string.IsNullOrWhiteSpace(input)) return null;
    var s = input.Trim();

    // 1) Nếu đã là JSON array hợp lệ -> parse & re-serialize chuẩn
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
      catch { /* rơi xuống CSV parse */ }
    }

    // 2) CSV -> array
    var parts = s.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                 .Select(x => x.Trim())
                 .Where(x => x.Length > 0)
                 .Distinct(StringComparer.OrdinalIgnoreCase)
                 .ToList();

    // Nếu người dùng chỉ gõ một từ không có dấu phẩy vẫn OK
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
      p = new DriverProfile
      {
        Id = NewId(),
        UserId = uid,
        FullName = "New Driver",
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
      p = new DriverProfile
      {
        Id = NewId(),
        UserId = uid,
        FullName = dto.FullName ?? "New Driver",
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
          return ApiResponse<DriverProfile>.Fail("IMG_URL_INVALID", "Ảnh đại diện không phải URL hợp lệ (http/https).");
        p.ImgUrl = normalized;
      }

      if (dto.IsAvailable is not null) p.IsAvailable = dto.IsAvailable.Value;

      p.UpdatedAt = DateTime.UtcNow;
    }

    await _db.SaveChangesAsync();
    return ApiResponse<DriverProfile>.Ok(p);
  }


  // ========= Public listing & detail =========
  // [HttpGet]
  // [SwaggerOperation(Summary = "List Drivers")]
  // [ProducesResponseType(typeof(ApiResponse<PageResult<DriverProfile>>), 200)]
  // public async Task<ActionResult<ApiResponse<PageResult<DriverProfile>>>> ListDrivers(
  //     [FromQuery] string? name = null,
  //     [FromQuery] string? skills = null,
  //     [FromQuery] string? location = null,
  //     [FromQuery] bool? isAvailable = null,
  //     [FromQuery] decimal? minRating = null,
  //     [FromQuery] int page = 1, [FromQuery] int size = 10, [FromQuery] string? sort = "rating:desc")
  // {
  //   var q = _db.DriverProfiles.AsQueryable();
  //   if (!string.IsNullOrWhiteSpace(name)) q = q.Where(d => d.FullName.Contains(name));
  //   if (!string.IsNullOrWhiteSpace(skills))
  //   {
  //     var parts = skills.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
  //     foreach (var p in parts) q = q.Where(d => d.Skills != null && d.Skills.Contains(p));
  //   }
  //   if (!string.IsNullOrWhiteSpace(location)) q = q.Where(d => d.Location == location);
  //   if (isAvailable.HasValue) q = q.Where(d => d.IsAvailable == isAvailable.Value);
  //   if (minRating.HasValue) q = q.Where(d => d.Rating >= minRating.Value);

  //   if (!string.IsNullOrWhiteSpace(sort))
  //   {
  //     var s = sort.Split(':'); var field = s[0]; var dir = s.Length > 1 ? s[1] : "asc";
  //     q = (field, dir) switch
  //     {
  //       ("name", "asc") => q.OrderBy(d => d.FullName),
  //       ("name", "desc") => q.OrderByDescending(d => d.FullName),
  //       ("rating", "asc") => q.OrderBy(d => d.Rating),
  //       ("rating", "desc") => q.OrderByDescending(d => d.Rating),
  //       _ => q.OrderByDescending(d => d.Rating)
  //     };
  //   }

  //   var total = await q.CountAsync();
  //   var items = await q.Skip((page - 1) * size).Take(size).ToListAsync();
  //   return ApiResponse<PageResult<DriverProfile>>.Ok(new PageResult<DriverProfile>
  //   {
  //     page = page,
  //     size = size,
  //     totalItems = total,
  //     totalPages = (int)Math.Ceiling(total / (double)size),
  //     hasNext = page * size < total,
  //     hasPrev = page > 1,
  //     items = items
  //   });
  // }

  [HttpGet]
  [SwaggerOperation(Summary = "List Drivers")]
  [ProducesResponseType(typeof(ApiResponse<PageResult<DriverForListDto>>), 200)]
  public async Task<ActionResult<ApiResponse<PageResult<DriverForListDto>>>> ListDrivers(
    [FromQuery] string? name = null,
    [FromQuery] string? skills = null,
    [FromQuery] string? location = null,
    [FromQuery] bool? isAvailable = null,
    [FromQuery] decimal? minRating = null,
    // NEW: cho phép ẩn driver đã hired
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

    // NEW: filter ẩn driver đã có quan hệ với bất kỳ company nào
    if (excludeHired == true)
    {
      q = q.Where(d => !_db.CompanyDriverRelations
                        .Any(r => r.DriverUserId == d.UserId));
    }

    // sort (giữ nguyên logic cũ)
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

    // Project sang DTO và tính IsHired bằng subquery Any()
    var items = await q
      .Skip((page - 1) * size)
      .Take(size)
      .Select(d => new DriverForListDto
      {
        Id = d.Id,
        UserId = d.UserId,
        FullName = d.FullName,
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
    // Chỉ cho phép owner tự nạp ví của mình
    var uid = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
    if (uid == null || uid != userId)
      return ApiResponse<object>.Fail("FORBIDDEN", "Bạn không có quyền nạp ví cho user này");

    if (dto.AmountCents <= 0)
      return ApiResponse<object>.Fail("VALIDATION", "AmountCents phải > 0");

    // Lấy (hoặc tạo) ví Driver
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

    // Cộng tiền
    wallet.BalanceCents += dto.AmountCents;
    wallet.UpdatedAt = DateTime.UtcNow;

    // Ghi transaction
    var tx = new Transaction
    {
      Id = Guid.NewGuid().ToString("N")[..24],
      FromWalletId = null,                 // topup: tiền vào hệ thống từ bên ngoài
      ToWalletId = wallet.Id,
      AmountCents = dto.AmountCents,
      Status = TxStatus.Completed,
      IdempotencyKey = dto.IdempotencyKey,
      CreatedAt = DateTime.UtcNow,
      Type = TxType.Topup,                 // đã có trong enum
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
    if (dto.AmountCents <= 0) return ApiResponse<object>.Fail("VALIDATION", "AmountCents phải > 0");

    var wallet = await _db.Wallets.FirstOrDefaultAsync(w => w.OwnerType == "Driver" && w.OwnerRefId == userId);
    if (wallet == null) return ApiResponse<object>.Fail("NO_WALLET", "Driver chưa có ví");              // (tùy chọn: thông điệp rõ ràng)
    if (wallet.BalanceCents < dto.AmountCents) return ApiResponse<object>.Fail("INSUFFICIENT_FUNDS", "Số dư không đủ");

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
      Type = TxType.Withdraw,                                      // <-- QUAN TRỌNG
      RefId = userId,                                              // (khuyến nghị)
      MetaJson = JsonSerializer.Serialize(new { driverUserId = userId, method = "manual" }) // (khuyến nghị)
    };

    _db.Transactions.Add(tx);
    await _db.SaveChangesAsync();

    return ApiResponse<object>.Ok(new { wallet.Id, balance = wallet.BalanceCents, transactionId = tx.Id }); // (khuyến nghị)
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
    if (string.IsNullOrWhiteSpace(dto.CompanyId)) return ApiResponse<JobApplication>.Fail("VALIDATION", "CompanyId bắt buộc");

    var dup = await _db.JobApplications.AnyAsync(a => a.CompanyId == dto.CompanyId && a.DriverUserId == userId && a.Status == ApplyStatus.Applied);
    if (dup) return ApiResponse<JobApplication>.Fail("DUPLICATE", "Bạn đã ứng tuyển và đang chờ xử lý");

    var alreadyHired = await _db.CompanyDriverRelations.AnyAsync(r => r.DriverUserId == userId);
    if (alreadyHired) return ApiResponse<JobApplication>.Fail("ALREADY_EMPLOYED", "Bạn đã là tài xế của một công ty, không thể ứng tuyển nơi khác.");

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
      return ApiResponse<object>.Fail("NOT_FOUND", "Application không tồn tại");

    if (app.Status != ApplyStatus.Applied)
      return ApiResponse<object>.Fail("INVALID_STATE", "Không thể huỷ application này");

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
    if (inv == null) return ApiResponse<object>.Fail("NOT_FOUND", "Invite không tồn tại");

    // giữ nguyên logic: chỉ xử lý khi Pending
    if (!string.Equals(inv.Status, "Pending", StringComparison.OrdinalIgnoreCase))
      return ApiResponse<object>.Fail("INVALID_STATE", "Invite đã được xử lý");

    // Transaction để mọi thay đổi đi cùng nhau
    await using var tx = await _db.Database.BeginTransactionAsync();
    try
    {
      // 1) Accept lời mời
      inv.Status = "Accepted";

      // 2) Nếu chưa có quan hệ company-driver thì tạo
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

      // 3) HỦY TẤT CẢ JobApplication đang Applied của driver này (mọi công ty)
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
    if (inv == null) return ApiResponse<object>.Fail("NOT_FOUND", "Invite không tồn tại");
    if (inv.Status != "Pending") return ApiResponse<object>.Fail("INVALID_STATE", "Invite đã được xử lý");

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
}
