using Api.Common;
using Api.Contracts.Companies;
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
using System.Linq.Expressions;



namespace Api.Controllers;

[ApiController]
[Route("api/companies")]
[SwaggerTag("Companies")]
public class CompaniesController : ControllerBase
{
  private readonly AppDbContext _db;
  public CompaniesController(AppDbContext db) => _db = db;

  // ===== Helper =====
  private string? GetUserId() =>
      User.FindFirstValue(ClaimTypes.NameIdentifier) ??
      User.FindFirstValue(ClaimTypes.Name) ??
      User.FindFirstValue("sub");

  private ActionResult<ApiResponse<T>> Forbidden<T>() =>
      ApiResponse<T>.Fail("FORBIDDEN", "Bạn không có quyền trên company này");

  private async Task<Company?> GetOwnedCompanyAsync(string companyId)
  {
    var uid = GetUserId();
    var c = await _db.Companies.FirstOrDefaultAsync(x => x.Id == companyId);
    if (c == null) return null;
    return c.OwnerUserId == uid ? c : null;
  }

  private async Task<Company?> GetCompanyOfCurrentUserAsync()
  {
    var uid = GetUserId();
    if (uid == null) return null;
    return await _db.Companies.FirstOrDefaultAsync(c => c.OwnerUserId == uid);
  }

  private static string NewId() => Guid.NewGuid().ToString("N")[..24];

  // ====== Helpers riêng cho Service (chuẩn hoá theo companyId + serviceId) ======
  private async Task<Service?> GetOwnedServiceAsync(string companyId, string serviceId)
  {
    // service thuộc company?
    var svc = await _db.Services.FirstOrDefaultAsync(s => s.Id == serviceId && s.CompanyId == companyId);
    if (svc == null) return null;

    // company có thuộc current user?
    var owned = await GetOwnedCompanyAsync(companyId);
    return owned != null ? svc : null;
  }

  // ========= Me =========
  [Authorize(Roles = "Company")]
  [HttpGet("me")]
  [SwaggerOperation(Summary = "Get My Company Profile (auto-create if missing)")]
  [ProducesResponseType(typeof(ApiResponse<Company>), 200)]
  public async Task<ActionResult<ApiResponse<Company>>> GetMyCompany()
  {
    var uid = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
    if (uid == null) return ApiResponse<Company>.Fail("UNAUTHORIZED", "Missing user id");

    var p = await _db.Companies.FirstOrDefaultAsync(x => x.OwnerUserId == uid);
    if (p == null)
    {
      p = new Company
      {
        Id = NewId(),
        OwnerUserId = uid,
        Name = "",
        Description = null,
        Rating = 0,
        Membership = "Free",
        MembershipExpiresAt = null,
        IsActive = true,
        ImgUrl = null,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
      };
      _db.Companies.Add(p);
      await _db.SaveChangesAsync();
    }

    return ApiResponse<Company>.Ok(p);
  }

  [Authorize(Roles = "Company")]
  [HttpPut("me")]
  [Consumes("application/json")]
  [SwaggerOperation(Summary = "Update My Company Profile (upsert)")]
  [ProducesResponseType(typeof(ApiResponse<Company>), 200)]
  public async Task<ActionResult<ApiResponse<Company>>> UpdateMyCompany([FromBody] UpdateCompanyDto dto)
  {
    var uid = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
    if (uid == null) return ApiResponse<Company>.Fail("UNAUTHORIZED", "Missing user id");

    var p = await _db.Companies.FirstOrDefaultAsync(x => x.OwnerUserId == uid);
    if (p == null)
    {
      p = new Company
      {
        Id = NewId(),
        OwnerUserId = uid,
        Name = dto.Name ?? "",
        Description = dto.Description,
        Rating = 0,
        Membership = "Free",
        MembershipExpiresAt = null,
        IsActive = true,
        ImgUrl = UrlHelper.TryNormalizeUrl(dto.ImgUrl),
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
      };
      _db.Companies.Add(p);
    }
    else
    {
      if (dto.Name is not null) p.Name = dto.Name;
      if (dto.Description is not null) p.Description = dto.Description;
      if (dto.ImgUrl is not null)
      {
        var normalized = UrlHelper.TryNormalizeUrl(dto.ImgUrl);
        if (normalized == null)
          return ApiResponse<Company>.Fail("IMG_URL_INVALID", "Ảnh đại diện không phải URL hợp lệ (http/https).");
        p.ImgUrl = normalized;
      }
      p.UpdatedAt = DateTime.UtcNow;
    }

    await _db.SaveChangesAsync();
    return ApiResponse<Company>.Ok(p);
  }

  [HttpGet]
  [SwaggerOperation(Summary = "List Companies")]
  [ProducesResponseType(typeof(ApiResponse<PageResult<Company>>), 200)]
  public async Task<ActionResult<ApiResponse<PageResult<Company>>>> ListCompanies(
    [FromQuery] string? name = null,
    [FromQuery] string? membership = null,
    [FromQuery] bool? isActive = true,
    [FromQuery] decimal? minRating = null,
    [FromQuery] decimal? maxRating = null,
    [FromQuery] int page = 1, [FromQuery] int size = 10, [FromQuery] string? sort = "name:asc")
  {
    var q = _db.Companies.AsQueryable();

    if (!string.IsNullOrWhiteSpace(name)) q = q.Where(c => c.Name.Contains(name));
    if (!string.IsNullOrWhiteSpace(membership)) q = q.Where(c => c.Membership == membership);
    if (isActive.HasValue) q = q.Where(c => c.IsActive == isActive.Value);
    if (minRating.HasValue) q = q.Where(c => c.Rating >= minRating.Value);
    if (maxRating.HasValue) q = q.Where(c => c.Rating <= maxRating.Value);

    // map rank: Premium(3) > Basic(2) > Free/khác(1)
    Expression<Func<Company, int>> membershipRank = c =>
      c.Membership == "Premium" ? 3 :
      c.Membership == "Basic" ? 2 : 1;

    // parsing sort
    var s = (sort ?? "name:asc").Split(':');
    var field = s[0];
    var dir = s.Length > 1 ? s[1] : "asc";

    // Luật:
    // - Nếu sort theo name/rating: luôn ưu tiên membershipRank DESC trước, sau đó mới theo field + dir.
    // - Nếu sort theo membership: tôn trọng dir (asc => Free…Premium, desc => Premium…Free)
    q = (field, dir) switch
    {
      ("name", "desc") => q.OrderByDescending(membershipRank).ThenByDescending(c => c.Name),
      ("name", _) => q.OrderByDescending(membershipRank).ThenBy(c => c.Name),

      ("rating", "desc") => q.OrderByDescending(membershipRank).ThenByDescending(c => c.Rating),
      ("rating", _) => q.OrderByDescending(membershipRank).ThenBy(c => c.Rating),

      // sort membership thuần theo dir
      ("membership", "desc") => q.OrderByDescending(membershipRank),
      ("membership", _) => q.OrderBy(membershipRank),

      // mặc định: name asc với ưu tiên membership
      _ => q.OrderByDescending(membershipRank).ThenBy(c => c.Name)
    };

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

  [HttpGet("{id}")]
  [SwaggerOperation(Summary = "Get Company by Id")]
  [ProducesResponseType(typeof(ApiResponse<Company>), 200)]
  public async Task<ActionResult<ApiResponse<Company>>> GetCompanyById([FromRoute] string id)
  {
    var c = await _db.Companies.FirstOrDefaultAsync(x => x.Id == id);
    if (c == null) return ApiResponse<Company>.Fail("NOT_FOUND", "Company không tồn tại");
    return ApiResponse<Company>.Ok(c);
  }

  // ========= List Company Drivers =========
  [Authorize(Roles = "Company")]
  [HttpGet("{companyId}/drivers")]
  [SwaggerOperation(Summary = "List drivers employed by this company")]
  [ProducesResponseType(typeof(ApiResponse<PageResult<CompanyDriverDto>>), 200)]
  public async Task<ActionResult<ApiResponse<PageResult<CompanyDriverDto>>>> ListCompanyDrivers(
    [FromRoute] string companyId,
    [FromQuery] string? name = null,
    [FromQuery] bool? isAvailable = null,
    [FromQuery] decimal? minRating = null,
    [FromQuery] int page = 1,
    [FromQuery] int size = 10,
    [FromQuery] string? sort = "joinedAt:desc")
  {
    var company = await GetOwnedCompanyAsync(companyId);
    if (company == null) return Forbidden<PageResult<CompanyDriverDto>>();

    var query = from rel in _db.CompanyDriverRelations
                join d in _db.DriverProfiles on rel.DriverUserId equals d.UserId
                where rel.CompanyId == companyId
                select new CompanyDriverDto
                {
                  UserId = d.UserId,
                  FullName = d.FullName,
                  Phone = d.Phone,
                  Bio = d.Bio,
                  ImgUrl = d.ImgUrl,
                  Rating = d.Rating,
                  Skills = d.Skills,
                  Location = d.Location,
                  IsAvailable = d.IsAvailable,
                  JoinedAt = rel.CreatedAt,
                  BaseSalaryCents = rel.BaseSalaryCents
                };

    if (!string.IsNullOrWhiteSpace(name))
      query = query.Where(x => x.FullName.Contains(name));
    if (isAvailable.HasValue)
      query = query.Where(x => x.IsAvailable == isAvailable.Value);
    if (minRating.HasValue)
      query = query.Where(x => x.Rating >= minRating.Value);

    // sort
    if (!string.IsNullOrWhiteSpace(sort))
    {
      var s = sort.Split(':'); var field = s[0]; var dir = s.Length > 1 ? s[1] : "desc";
      query = (field, dir) switch
      {
        ("rating", "asc") => query.OrderBy(x => x.Rating),
        ("rating", "desc") => query.OrderByDescending(x => x.Rating),
        ("name", "asc") => query.OrderBy(x => x.FullName),
        ("name", "desc") => query.OrderByDescending(x => x.FullName),
        ("joinedAt", "asc") => query.OrderBy(x => x.JoinedAt),
        _ => query.OrderByDescending(x => x.JoinedAt)
      };
    }

    var total = await query.CountAsync();
    var items = await query.Skip((page - 1) * size).Take(size).ToListAsync();

    return ApiResponse<PageResult<CompanyDriverDto>>.Ok(new PageResult<CompanyDriverDto>
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

  // ========= SERVICES =========
  // CREATE
  [Authorize(Roles = "Company")]
  [HttpPost("{companyId}/services")]
  [SwaggerOperation(Summary = "Create Company Service")]
  [ProducesResponseType(typeof(ApiResponse<Service>), 200)]
  public async Task<ActionResult<ApiResponse<Service>>> CreateService(
    [FromRoute] string companyId,
    [FromBody] AddServiceDto dto)
  {
    var company = await GetOwnedCompanyAsync(companyId);
    if (company == null) return Forbidden<Service>();

    // Validate
    if (string.IsNullOrWhiteSpace(dto.Title))
      return ApiResponse<Service>.Fail("VALIDATION", "Title bắt buộc");
    if (dto.PriceCents <= 0)
      return ApiResponse<Service>.Fail("VALIDATION", "PriceCents phải > 0");

    // Normalize ImgUrl (nếu có)
    string? normalizedImgUrl = null;
    if (!string.IsNullOrWhiteSpace(dto.ImgUrl))
    {
      normalizedImgUrl = UrlHelper.TryNormalizeUrl(dto.ImgUrl);
      if (normalizedImgUrl == null)
        return ApiResponse<Service>.Fail("IMG_URL_INVALID", "ImgUrl không hợp lệ (http/https).");
    }

    var now = DateTime.UtcNow;

    var svc = new Service
    {
      Id = NewId(),
      CompanyId = company.Id,
      Title = dto.Title!.Trim(),
      Description = dto.Description,
      ImgUrl = normalizedImgUrl,               // <-- gán tại đây
      PriceCents = dto.PriceCents,
      IsActive = dto.IsActive ?? true,
      CreatedAt = now,
      UpdatedAt = now
    };

    _db.Services.Add(svc);
    await _db.SaveChangesAsync();
    return ApiResponse<Service>.Ok(svc);
  }


  // LIST (của chính company hiện tại)
  [Authorize(Roles = "Company")]
  [HttpGet("{companyId}/services")]
  [SwaggerOperation(Summary = "List Company Services")]
  [ProducesResponseType(typeof(ApiResponse<PageResult<Service>>), 200)]
  public async Task<ActionResult<ApiResponse<PageResult<Service>>>> ListServicesOfCompany(
      [FromRoute] string companyId,
      [FromQuery] bool? isActive = null,
      [FromQuery] string? q = null,
      [FromQuery] int page = 1,
      [FromQuery] int size = 10,
      [FromQuery] string? sort = "updatedAt:desc")
  {
    var owned = await GetOwnedCompanyAsync(companyId);
    if (owned == null) return Forbidden<PageResult<Service>>();

    var query = _db.Services.Where(s => s.CompanyId == companyId);
    if (isActive.HasValue) query = query.Where(s => s.IsActive == isActive.Value);
    if (!string.IsNullOrWhiteSpace(q))
      query = query.Where(s => s.Title.Contains(q) || (s.Description != null && s.Description.Contains(q)));

    if (!string.IsNullOrWhiteSpace(sort))
    {
      var s = sort.Split(':'); var field = s[0]; var dir = s.Length > 1 ? s[1] : "desc";
      query = (field, dir) switch
      {
        ("price", "asc") => query.OrderBy(sv => sv.PriceCents),
        ("price", "desc") => query.OrderByDescending(sv => sv.PriceCents),
        ("title", "asc") => query.OrderBy(sv => sv.Title),
        ("title", "desc") => query.OrderByDescending(sv => sv.Title),
        ("createdAt", "asc") => query.OrderBy(sv => sv.CreatedAt),
        ("createdAt", "desc") => query.OrderByDescending(sv => sv.CreatedAt),
        ("updatedAt", "asc") => query.OrderBy(sv => sv.UpdatedAt),
        _ => query.OrderByDescending(sv => sv.UpdatedAt)
      };
    }

    var total = await query.CountAsync();
    var items = await query.Skip((page - 1) * size).Take(size).ToListAsync();

    return ApiResponse<PageResult<Service>>.Ok(new PageResult<Service>
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

  [AllowAnonymous] // hoặc [Authorize] nếu bạn muốn yêu cầu đăng nhập
  [HttpGet("{companyId}/services/public")]
  [SwaggerOperation(Summary = "Public: List Active Services of a Company")]
  [ProducesResponseType(typeof(ApiResponse<PageResult<Service>>), 200)]
  public async Task<ActionResult<ApiResponse<PageResult<Service>>>> ListServicesPublic(
    [FromRoute] string companyId,
    [FromQuery] bool? isActive = true, // default: chỉ lấy active
    [FromQuery] string? q = null,
    [FromQuery] int page = 1,
    [FromQuery] int size = 10,
    [FromQuery] string? sort = "updatedAt:desc")
  {
    var query = _db.Services.Where(s => s.CompanyId == companyId);

    if (isActive.HasValue) query = query.Where(s => s.IsActive == isActive.Value);
    if (!string.IsNullOrWhiteSpace(q))
      query = query.Where(s => s.Title.Contains(q) || (s.Description != null && s.Description.Contains(q)));

    if (!string.IsNullOrWhiteSpace(sort))
    {
      var s = sort.Split(':'); var field = s[0]; var dir = s.Length > 1 ? s[1] : "desc";
      query = (field, dir) switch
      {
        ("price", "asc") => query.OrderBy(sv => sv.PriceCents),
        ("price", "desc") => query.OrderByDescending(sv => sv.PriceCents),
        ("title", "asc") => query.OrderBy(sv => sv.Title),
        ("title", "desc") => query.OrderByDescending(sv => sv.Title),
        ("createdAt", "asc") => query.OrderBy(sv => sv.CreatedAt),
        ("createdAt", "desc") => query.OrderByDescending(sv => sv.CreatedAt),
        ("updatedAt", "asc") => query.OrderBy(sv => sv.UpdatedAt),
        _ => query.OrderByDescending(sv => sv.UpdatedAt)
      };
    }

    var total = await query.CountAsync();
    var items = await query.Skip((page - 1) * size).Take(size).ToListAsync();

    return ApiResponse<PageResult<Service>>.Ok(new PageResult<Service>
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

  // (OPTIONAL) GET DETAIL
  [Authorize(Roles = "Company")]
  [HttpGet("{companyId}/services/{serviceId}")]
  [SwaggerOperation(Summary = "Get Service Detail")]
  [ProducesResponseType(typeof(ApiResponse<Service>), 200)]
  public async Task<ActionResult<ApiResponse<Service>>> GetServiceDetail(
      [FromRoute] string companyId,
      [FromRoute] string serviceId)
  {
    var svc = await GetOwnedServiceAsync(companyId, serviceId);
    if (svc == null) return Forbidden<Service>();
    return ApiResponse<Service>.Ok(svc);
  }

  // UPDATE
  [Authorize(Roles = "Company")]
  [HttpPut("{companyId}/services/{serviceId}")]
  [Consumes("application/json")]
  [SwaggerOperation(Summary = "Update Service")]
  [ProducesResponseType(typeof(ApiResponse<Service>), 200)]
  public async Task<ActionResult<ApiResponse<Service>>> UpdateService(
    [FromRoute] string companyId,
    [FromRoute] string serviceId,
    [FromBody] UpdateServiceDto dto)
  {
    var svc = await GetOwnedServiceAsync(companyId, serviceId);
    if (svc == null) return Forbidden<Service>();

    if (dto.Title is not null) svc.Title = dto.Title;
    if (dto.Description is not null) svc.Description = dto.Description;

    // NEW: ImgUrl — cho phép xóa ("" => null) hoặc set mới (cần là http/https hợp lệ)
    if (dto.ImgUrl is not null)
    {
      if (string.IsNullOrWhiteSpace(dto.ImgUrl))
      {
        // xóa ảnh
        svc.ImgUrl = null;
      }
      else
      {
        var normalized = UrlHelper.TryNormalizeUrl(dto.ImgUrl);
        if (normalized == null)
          return ApiResponse<Service>.Fail("IMG_URL_INVALID", "ImgUrl không hợp lệ (http/https).");
        svc.ImgUrl = normalized;
      }
    }

    if (dto.PriceCents.HasValue)
    {
      if (dto.PriceCents.Value <= 0)
        return ApiResponse<Service>.Fail("VALIDATION", "PriceCents phải > 0");
      svc.PriceCents = dto.PriceCents.Value;
    }

    if (dto.IsActive.HasValue) svc.IsActive = dto.IsActive.Value;

    svc.UpdatedAt = DateTime.UtcNow;
    await _db.SaveChangesAsync();
    return ApiResponse<Service>.Ok(svc);
  }


  // PAUSE
  [Authorize(Roles = "Company")]
  [HttpPost("{companyId}/services/{serviceId}/pause")]
  [SwaggerOperation(Summary = "Pause Service")]
  [ProducesResponseType(typeof(ApiResponse<Service>), 200)]
  public async Task<ActionResult<ApiResponse<Service>>> PauseService(
      [FromRoute] string companyId,
      [FromRoute] string serviceId)
  {
    var svc = await GetOwnedServiceAsync(companyId, serviceId);
    if (svc == null) return Forbidden<Service>();
    if (!svc.IsActive) return ApiResponse<Service>.Ok(svc); // idempotent

    svc.IsActive = false;
    svc.UpdatedAt = DateTime.UtcNow;
    await _db.SaveChangesAsync();
    return ApiResponse<Service>.Ok(svc);
  }

  // REACTIVATE
  [Authorize(Roles = "Company")]
  [HttpPost("{companyId}/services/{serviceId}/reactivate")]
  [SwaggerOperation(Summary = "Reactivate Service")]
  [ProducesResponseType(typeof(ApiResponse<Service>), 200)]
  public async Task<ActionResult<ApiResponse<Service>>> ReactivateService(
      [FromRoute] string companyId,
      [FromRoute] string serviceId)
  {
    var svc = await GetOwnedServiceAsync(companyId, serviceId);
    if (svc == null) return Forbidden<Service>();
    if (svc.IsActive) return ApiResponse<Service>.Ok(svc); // idempotent

    svc.IsActive = true;
    svc.UpdatedAt = DateTime.UtcNow;
    await _db.SaveChangesAsync();
    return ApiResponse<Service>.Ok(svc);
  }

  // ========= Wallet =========
  [Authorize(Roles = "Company")]
  [HttpGet("{id}/wallet")]
  [SwaggerOperation(Summary = "Get Company Wallet")]
  [ProducesResponseType(typeof(ApiResponse<Wallet>), 200)]
  public async Task<ActionResult<ApiResponse<Wallet>>> GetWallet([FromRoute] string id)
  {
    var company = await GetOwnedCompanyAsync(id);
    if (company == null) return Forbidden<Wallet>();

    var wallet = await _db.Wallets.FirstOrDefaultAsync(w => w.OwnerType == "Company" && w.OwnerRefId == id);
    if (wallet == null)
    {
      wallet = new Wallet
      {
        Id = NewId(),
        OwnerType = "Company",
        OwnerRefId = id,
        BalanceCents = 0,
        LowBalanceThreshold = 10000,
        UpdatedAt = DateTime.UtcNow
      };
      _db.Wallets.Add(wallet);
      await _db.SaveChangesAsync();
    }
    return ApiResponse<Wallet>.Ok(wallet);
  }


  [Authorize(Roles = "Company")]
  [HttpPost("{id}/wallet/withdraw")]
  [SwaggerOperation(Summary = "Withdraw from Company Wallet")]
  [ProducesResponseType(typeof(ApiResponse<object>), 200)]
  public async Task<ActionResult<ApiResponse<object>>> Withdraw(
    [FromRoute] string id,
    [FromBody] WithdrawDto dto)
  {
    var company = await GetOwnedCompanyAsync(id);
    if (company == null) return Forbidden<object>();

    if (dto.AmountCents <= 0)
      return ApiResponse<object>.Fail("VALIDATION", "AmountCents phải > 0");

    var wallet = await _db.Wallets.FirstOrDefaultAsync(
        w => w.OwnerType == "Company" && w.OwnerRefId == id);
    if (wallet == null)
      return ApiResponse<object>.Fail("NO_WALLET", "Company chưa có ví");

    if (wallet.BalanceCents < dto.AmountCents)
      return ApiResponse<object>.Fail("INSUFFICIENT_FUNDS", "Số dư không đủ");

    // Idempotency
    if (!string.IsNullOrWhiteSpace(dto.IdempotencyKey))
    {
      var exists = await _db.Transactions.AnyAsync(t => t.IdempotencyKey == dto.IdempotencyKey);
      if (exists) return ApiResponse<object>.Ok(new { balance = wallet.BalanceCents });
    }

    // Trừ tiền
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
      CreatedAt = DateTime.UtcNow,
      Type = TxType.Withdraw,
      RefId = company.Id,
      MetaJson = JsonSerializer.Serialize(new
      {
        companyId = company.Id,
        method = "manual",
      })
    };

    _db.Transactions.Add(tx);
    await _db.SaveChangesAsync();

    return ApiResponse<object>.Ok(new
    {
      transactionId = tx.Id,
      balance = wallet.BalanceCents
    });
  }

  // ========= Topup =========
  [Authorize(Roles = "Company")]
  [HttpPost("{id}/wallet/topup")]
  [SwaggerOperation(Summary = "Topup Company Wallet")]
  [ProducesResponseType(typeof(ApiResponse<object>), 200)]
  public async Task<ActionResult<ApiResponse<object>>> Topup([FromRoute] string id, [FromBody] TopupDto dto)
  {
    var company = await GetOwnedCompanyAsync(id);
    if (company == null) return Forbidden<object>();
    if (dto.AmountCents <= 0) return ApiResponse<object>.Fail("VALIDATION", "AmountCents phải > 0");

    var wallet = await _db.Wallets.FirstOrDefaultAsync(w => w.OwnerType == "Company" && w.OwnerRefId == id);
    if (wallet == null)
    {
      wallet = new Wallet
      {
        Id = NewId(),
        OwnerType = "Company",
        OwnerRefId = id,
        BalanceCents = 0,
        LowBalanceThreshold = 10000,
        UpdatedAt = DateTime.UtcNow
      };
      _db.Wallets.Add(wallet);
    }

    if (!string.IsNullOrWhiteSpace(dto.IdempotencyKey))
    {
      var exists = await _db.Transactions.AnyAsync(t => t.IdempotencyKey == dto.IdempotencyKey);
      if (exists) return ApiResponse<object>.Ok(new { balance = wallet.BalanceCents });
    }

    wallet.BalanceCents += dto.AmountCents;
    wallet.UpdatedAt = DateTime.UtcNow;

    var tx = new Transaction
    {
      Id = NewId(),
      FromWalletId = null,
      ToWalletId = wallet.Id,
      AmountCents = dto.AmountCents,
      Status = TxStatus.Completed,
      IdempotencyKey = dto.IdempotencyKey,
      CreatedAt = DateTime.UtcNow,
      Type = TxType.Topup,
      RefId = company.Id,
      MetaJson = JsonSerializer.Serialize(new { companyId = company.Id, source = "manual" })
    };
    _db.Transactions.Add(tx);

    await _db.SaveChangesAsync();
    return ApiResponse<object>.Ok(new { wallet.Id, balance = wallet.BalanceCents });
  }

  // ========= Transactions =========
  [Authorize(Roles = "Company")]
  [HttpGet("{id}/transactions")]
  [SwaggerOperation(Summary = "List Company Transactions")]
  [ProducesResponseType(typeof(ApiResponse<PageResult<Transaction>>), 200)]
  public async Task<ActionResult<ApiResponse<PageResult<Transaction>>>> GetTransactions(
      [FromRoute] string id, [FromQuery] int page = 1, [FromQuery] int size = 10)
  {
    var company = await GetOwnedCompanyAsync(id);
    if (company == null) return Forbidden<PageResult<Transaction>>();

    var wallet = await _db.Wallets.FirstOrDefaultAsync(w => w.OwnerType == "Company" && w.OwnerRefId == id);
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


  // ========= Pay Salary =========
  [Authorize(Roles = "Company")]
  [HttpPost("{id}/pay-salary")]
  [SwaggerOperation(Summary = "Pay Driver Salary")]
  [ProducesResponseType(typeof(ApiResponse<object>), 200)]
  public async Task<ActionResult<ApiResponse<object>>> PaySalary(
  [FromRoute] string id, [FromBody] PaySalaryDto dto)
  {
    var company = await GetOwnedCompanyAsync(id);
    if (company == null) return Forbidden<object>();
    if (dto.AmountCents <= 0) return ApiResponse<object>.Fail("VALIDATION", "AmountCents phải > 0");

    // Kiểm tra quan hệ employment
    var employed = await _db.CompanyDriverRelations
      .AnyAsync(r => r.CompanyId == id && r.DriverUserId == dto.DriverUserId);
    if (!employed) return ApiResponse<object>.Fail("NOT_EMPLOYED", "Driver chưa/không làm việc cho công ty.");

    var period = !string.IsNullOrWhiteSpace(dto.Period)
      ? dto.Period!.Trim()
      : DateTime.UtcNow.ToString("yyyy-MM");

    var companyWallet = await _db.Wallets.FirstOrDefaultAsync(w => w.OwnerType == "Company" && w.OwnerRefId == id);
    if (companyWallet == null) return ApiResponse<object>.Fail("NO_WALLET", "Company chưa có ví");

    var driverWallet = await _db.Wallets.FirstOrDefaultAsync(w => w.OwnerType == "Driver" && w.OwnerRefId == dto.DriverUserId);
    if (driverWallet == null)
    {
      driverWallet = new Wallet
      {
        Id = NewId(),
        OwnerType = "Driver",
        OwnerRefId = dto.DriverUserId,
        BalanceCents = 0,
        LowBalanceThreshold = 10000,
        UpdatedAt = DateTime.UtcNow
      };
      _db.Wallets.Add(driverWallet);
      await _db.SaveChangesAsync();
    }

    if (companyWallet.BalanceCents < dto.AmountCents)
      return ApiResponse<object>.Fail("INSUFFICIENT_FUNDS", "Số dư không đủ");

    // Chuẩn hoá idempotency key
    var idem = string.IsNullOrWhiteSpace(dto.IdempotencyKey)
      ? $"salary:{id}:{dto.DriverUserId}:{period}:{dto.AmountCents}"
      : dto.IdempotencyKey!.Trim();

    // Idempotency check
    var existed = await _db.Transactions.AnyAsync(t =>
      t.Type == TxType.PaySalary && t.IdempotencyKey == idem && t.Status == TxStatus.Completed);
    if (existed)
      return ApiResponse<object>.Ok(new { reused = true });

    using var txScope = await _db.Database.BeginTransactionAsync();
    try
    {
      companyWallet.BalanceCents -= dto.AmountCents;
      companyWallet.UpdatedAt = DateTime.UtcNow;
      driverWallet.BalanceCents += dto.AmountCents;
      driverWallet.UpdatedAt = DateTime.UtcNow;

      var meta = new
      {
        companyId = company.Id,
        driverUserId = dto.DriverUserId,
        period,
        note = dto.Note
      };

      _db.Transactions.Add(new Transaction
      {
        Id = NewId(),
        FromWalletId = companyWallet.Id,
        ToWalletId = driverWallet.Id,
        AmountCents = dto.AmountCents,
        Status = TxStatus.Completed,
        IdempotencyKey = idem,
        CreatedAt = DateTime.UtcNow,
        Type = TxType.PaySalary,
        RefId = dto.DriverUserId,   // giữ nguyên cách bạn đang dùng
        MetaJson = JsonSerializer.Serialize(meta)
      });

      await _db.SaveChangesAsync();
      await txScope.CommitAsync();

      return ApiResponse<object>.Ok(new
      {
        companyBalance = companyWallet.BalanceCents,
        driverBalance = driverWallet.BalanceCents,
        period
      });
    }
    catch
    {
      await txScope.RollbackAsync();
      return ApiResponse<object>.Fail("INTERNAL_ERROR", "Payout failed");
    }
  }


  // ========= Pay Membership =========
  [Authorize(Roles = "Company")]
  [HttpPost("{id}/pay-membership")]
  [SwaggerOperation(Summary = "Pay Membership")]
  [ProducesResponseType(typeof(ApiResponse<object>), 200)]
  public async Task<ActionResult<ApiResponse<object>>> PayMembership([FromRoute] string id, [FromBody] PayMembershipDto dto)
  {
    var company = await GetOwnedCompanyAsync(id);
    if (company == null) return Forbidden<object>();
    if (dto.AmountCents <= 0) return ApiResponse<object>.Fail("VALIDATION", "AmountCents phải > 0");
    if (string.IsNullOrWhiteSpace(dto.Plan)) return ApiResponse<object>.Fail("VALIDATION", "Plan bắt buộc");

    var wallet = await _db.Wallets.FirstOrDefaultAsync(w => w.OwnerType == "Company" && w.OwnerRefId == id);
    if (wallet == null || wallet.BalanceCents < dto.AmountCents)
      return ApiResponse<object>.Fail("INSUFFICIENT_FUNDS", "Số dư không đủ");

    wallet.BalanceCents -= dto.AmountCents;
    wallet.UpdatedAt = DateTime.UtcNow;

    company.Membership = dto.Plan!;
    company.MembershipExpiresAt = (company.MembershipExpiresAt ?? DateTime.UtcNow).AddDays(30);
    company.UpdatedAt = DateTime.UtcNow;

    var tx = new Transaction
    {
      Id = NewId(),
      FromWalletId = wallet.Id,
      ToWalletId = "69w000000000000000000001",
      AmountCents = dto.AmountCents,
      Status = TxStatus.Completed,
      IdempotencyKey = dto.IdempotencyKey,
      CreatedAt = DateTime.UtcNow,
      Type = TxType.PayMembership,
      RefId = company.Id,
      MetaJson = JsonSerializer.Serialize(new { plan = dto.Plan })
    };
    _db.Transactions.Add(tx);

    await _db.SaveChangesAsync();
    return ApiResponse<object>.Ok(new
    {
      membership = company.Membership,
      expiresAt = company.MembershipExpiresAt,
      balance = wallet.BalanceCents
    });
  }

  // ========= Applications =========
  [Authorize(Roles = "Company")]
  [HttpGet("{id}/applications")]
  [SwaggerOperation(Summary = "List Job Applications")]
  [ProducesResponseType(typeof(ApiResponse<PageResult<JobApplication>>), 200)]
  public async Task<ActionResult<ApiResponse<PageResult<JobApplication>>>> ListApplications(
      [FromRoute] string id, [FromQuery] int page = 1, [FromQuery] int size = 10, [FromQuery] string? status = null)
  {
    var company = await GetOwnedCompanyAsync(id);
    if (company == null) return Forbidden<PageResult<JobApplication>>();

    var q = _db.JobApplications.Where(a => a.CompanyId == id);
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

  [Authorize(Roles = "Company")]
  [HttpPost("{id}/applications/{appId}/approve")]
  [SwaggerOperation(Summary = "Approve Job Application")]
  [ProducesResponseType(typeof(ApiResponse<object>), 200)]
  public async Task<ActionResult<ApiResponse<object>>> ApproveApplication([FromRoute] string id, [FromRoute] string appId)
  {
    var company = await GetOwnedCompanyAsync(id);
    if (company == null) return Forbidden<object>();

    var app = await _db.JobApplications.FirstOrDefaultAsync(a => a.Id == appId && a.CompanyId == id);
    if (app == null) return ApiResponse<object>.Fail("NOT_FOUND", "Application không tồn tại");
    if (app.Status != ApplyStatus.Applied) return ApiResponse<object>.Fail("INVALID_STATE", "Chỉ duyệt khi trạng thái Applied");

    app.Status = ApplyStatus.Accepted;

    var exists = await _db.CompanyDriverRelations.AnyAsync(r => r.CompanyId == id && r.DriverUserId == app.DriverUserId);
    if (!exists)
    {
      _db.CompanyDriverRelations.Add(new CompanyDriverRelation
      {
        Id = NewId(),
        CompanyId = id,
        DriverUserId = app.DriverUserId,
        BaseSalaryCents = 0,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
      });
    }

    await _db.SaveChangesAsync();
    return ApiResponse<object>.Ok(new { app.Id, app.Status });
  }

  [Authorize(Roles = "Company")]
  [HttpPost("{id}/applications/{appId}/reject")]
  [SwaggerOperation(Summary = "Reject Job Application")]
  [ProducesResponseType(typeof(ApiResponse<object>), 200)]
  public async Task<ActionResult<ApiResponse<object>>> RejectApplication([FromRoute] string id, [FromRoute] string appId)
  {
    var company = await GetOwnedCompanyAsync(id);
    if (company == null) return Forbidden<object>();

    var app = await _db.JobApplications.FirstOrDefaultAsync(a => a.Id == appId && a.CompanyId == id);
    if (app == null) return ApiResponse<object>.Fail("NOT_FOUND", "Application không tồn tại");
    if (app.Status != ApplyStatus.Applied) return ApiResponse<object>.Fail("INVALID_STATE", "Chỉ từ chối khi trạng thái Applied");

    app.Status = ApplyStatus.Rejected;
    await _db.SaveChangesAsync();
    return ApiResponse<object>.Ok(new { app.Id, app.Status });
  }

  // ========= Invitations =========
  [Authorize(Roles = "Company")]
  [HttpPost("{id}/invitations")]
  [SwaggerOperation(Summary = "Invite Driver (idempotent for active invites)")]
  [ProducesResponseType(typeof(ApiResponse<Invite>), 200)]
  public async Task<ActionResult<ApiResponse<Invite>>> InviteDriver(
    [FromRoute] string id,
    [FromBody] InviteDriverDto dto)
  {
    var company = await GetOwnedCompanyAsync(id);
    if (company == null) return Forbidden<Invite>();

    if (string.IsNullOrWhiteSpace(dto.DriverUserId))
      return ApiResponse<Invite>.Fail("VALIDATION", "DriverUserId bắt buộc");
    if (dto.BaseSalaryCents < 0)
      return ApiResponse<Invite>.Fail("VALIDATION", "BaseSalaryCents không âm");

    var now = DateTime.UtcNow;

    // Lấy invite gần nhất giữa company-driver
    var latest = await _db.Invites
      .Where(i => i.CompanyId == id && i.DriverUserId == dto.DriverUserId)
      .OrderByDescending(i => i.CreatedAt)
      .FirstOrDefaultAsync();

    if (latest != null)
    {
      // Nếu đang Pending & chưa hết hạn => idempotent: trả lại invite cũ
      if (latest.Status == "Pending")
      {
        var isExpired = latest.ExpiresAt.HasValue && latest.ExpiresAt.Value <= now;
        if (!isExpired)
        {
          // có thể cân nhắc update lương/hạn mời nếu muốn "refresh" (tùy policy)
          return ApiResponse<Invite>.Ok(latest);
        }

        // đã hết hạn => auto mark Expired
        latest.Status = "Expired";
        await _db.SaveChangesAsync();
      }

      // Nếu đã Accepted rồi thì không mời lại
      if (latest.Status == "Accepted")
      {
        return ApiResponse<Invite>.Fail(
          "ALREADY_ACCEPTED",
          "Tài xế đã chấp nhận lời mời trước đó.");
      }
      // Nếu Rejected/Cancelled/Expired: cho phép tạo lời mời mới
    }

    // Tạo invite mới
    var inv = new Invite
    {
      Id = NewId(),
      CompanyId = id,
      DriverUserId = dto.DriverUserId!,
      BaseSalaryCents = dto.BaseSalaryCents,
      Status = "Pending",
      CreatedAt = now,
      ExpiresAt = dto.ExpiresAt
    };

    _db.Invites.Add(inv);
    await _db.SaveChangesAsync();

    return ApiResponse<Invite>.Ok(inv);
  }


  // ========= Invitations: Cancel =========
  [Authorize(Roles = "Company")]
  [HttpPost("{id}/invitations/{inviteId}/cancel")]
  [SwaggerOperation(Summary = "Cancel Invitation")]
  [ProducesResponseType(typeof(ApiResponse<object>), 200)]
  public async Task<ActionResult<ApiResponse<object>>> CancelInvitation(
    [FromRoute] string id,
    [FromRoute] string inviteId)
  {
    // Chỉ chủ company mới được thao tác
    var company = await GetOwnedCompanyAsync(id);
    if (company == null) return Forbidden<object>();

    // Tìm invite thuộc company
    var inv = await _db.Invites.FirstOrDefaultAsync(i => i.Id == inviteId && i.CompanyId == id);
    if (inv == null) return ApiResponse<object>.Fail("NOT_FOUND", "Invite không tồn tại");

    // Chỉ cho phép thu hồi khi đang ở trạng thái pending
    if (!string.Equals(inv.Status, "Pending", StringComparison.OrdinalIgnoreCase))
      return ApiResponse<object>.Fail("INVALID_STATE", "Chỉ thu hồi được khi invite đang ở trạng thái Pending");

    inv.Status = "Cancelled";
    await _db.SaveChangesAsync();

    return ApiResponse<object>.Ok(new { inviteId = inv.Id, status = inv.Status });
  }


  [Authorize(Roles = "Company")]
  [HttpGet("{id}/invitations")]
  [SwaggerOperation(Summary = "List Invitations")]
  [ProducesResponseType(typeof(ApiResponse<PageResult<Invite>>), 200)]
  public async Task<ActionResult<ApiResponse<PageResult<Invite>>>> ListInvitations(
    [FromRoute] string id,
    [FromQuery] int page = 1,
    [FromQuery] int size = 10,
    [FromQuery] string? status = null)
  {
    var company = await GetOwnedCompanyAsync(id);
    if (company == null) return Forbidden<PageResult<Invite>>();

    var q = _db.Invites.Where(i => i.CompanyId == id);

    // filter theo status nếu có
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

  // ========= Orders (Company actions) =========

  [Authorize(Roles = "Company")]
  [HttpPost("orders/{orderId}/confirm")]
  [SwaggerOperation(Summary = "Confirm Order (Company)", Description = "Company xác nhận nhận đơn — chuyển trạng thái từ Pending -> InProgress.")]
  [ProducesResponseType(typeof(ApiResponse<Order>), 200)]
  public async Task<ActionResult<ApiResponse<Order>>> ConfirmOrder([FromRoute] string orderId)
  {
    var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == orderId);
    if (order == null) return ApiResponse<Order>.Fail("NOT_FOUND", "Order không tồn tại");

    // Chỉ cho phép nếu company hiện tại là owner của order
    var owned = await GetOwnedCompanyAsync(order.CompanyId);
    if (owned == null) return Forbidden<Order>();

    if (order.Status == OrderStatus.Completed)
      return ApiResponse<Order>.Fail("INVALID_STATE", "Order đã hoàn tất");

    if (order.Status == OrderStatus.Cancelled)
      return ApiResponse<Order>.Fail("INVALID_STATE", "Order đã bị hủy");

    if (order.Status != OrderStatus.Pending)
      return ApiResponse<Order>.Fail("INVALID_STATE", "Chỉ xác nhận được khi order đang ở trạng thái Pending");

    order.Status = OrderStatus.InProgress;
    order.UpdatedAt = DateTime.UtcNow;
    await _db.SaveChangesAsync();

    return ApiResponse<Order>.Ok(order);
  }

  [Authorize(Roles = "Company")]
  [HttpPost("orders/{orderId}/complete")]
  [SwaggerOperation(
      Summary = "Complete Order (Company)",
      Description = "Company hoàn tất đơn — chuyển InProgress -> Completed và **thu tiền** từ ví Rider sang ví Company (mock payment)."
  )]
  [ProducesResponseType(typeof(ApiResponse<object>), 200)]
  public async Task<ActionResult<ApiResponse<object>>> CompleteOrder([FromRoute] string orderId)
  {
    var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == orderId);
    if (order == null) return ApiResponse<object>.Fail("NOT_FOUND", "Order không tồn tại");

    // Chỉ cho phép nếu company hiện tại là owner của order
    var company = await GetOwnedCompanyAsync(order.CompanyId);
    if (company == null) return Forbidden<object>();

    if (order.Status == OrderStatus.Cancelled)
      return ApiResponse<object>.Fail("INVALID_STATE", "Order đã bị hủy");

    if (order.Status == OrderStatus.Completed)
      return ApiResponse<object>.Ok(new { message = "Order đã hoàn tất trước đó" });

    if (order.Status != OrderStatus.InProgress && order.Status != OrderStatus.Pending)
      return ApiResponse<object>.Fail("INVALID_STATE", "Chỉ hoàn tất được khi order ở trạng thái InProgress hoặc Pending");

    // Ví Rider
    var riderWallet = await _db.Wallets.FirstOrDefaultAsync(w => w.OwnerType == "Rider" && w.OwnerRefId == order.RiderUserId);
    if (riderWallet == null)
    {
      riderWallet = new Wallet
      {
        Id = NewId(),
        OwnerType = "Rider",
        OwnerRefId = order.RiderUserId,
        BalanceCents = 0,
        LowBalanceThreshold = 10000,
        UpdatedAt = DateTime.UtcNow
      };
      _db.Wallets.Add(riderWallet);
      // rider mới tạo ví chắc chắn không đủ tiền
    }

    // Ví Company
    var companyWallet = await _db.Wallets.FirstOrDefaultAsync(w => w.OwnerType == "Company" && w.OwnerRefId == company.Id);
    if (companyWallet == null)
    {
      companyWallet = new Wallet
      {
        Id = NewId(),
        OwnerType = "Company",
        OwnerRefId = company.Id,
        BalanceCents = 0,
        LowBalanceThreshold = 10000,
        UpdatedAt = DateTime.UtcNow
      };
      _db.Wallets.Add(companyWallet);
    }

    // Kiểm tra số dư Rider
    var amount = order.PriceCents;
    if (riderWallet.BalanceCents < amount)
      return ApiResponse<object>.Fail("INSUFFICIENT_FUNDS", "Số dư ví Rider không đủ để thanh toán order");

    // Chuyển tiền (mock payment)
    riderWallet.BalanceCents -= amount;
    riderWallet.UpdatedAt = DateTime.UtcNow;

    companyWallet.BalanceCents += amount;
    companyWallet.UpdatedAt = DateTime.UtcNow;

    var tx = new Transaction
    {
      Id = NewId(),
      FromWalletId = riderWallet.Id,
      ToWalletId = companyWallet.Id,
      AmountCents = amount,
      Status = TxStatus.Completed,
      IdempotencyKey = $"complete-order-{order.Id}",
      CreatedAt = DateTime.UtcNow,
      Type = TxType.OrderPayment,
      RefId = order.Id,
      MetaJson = JsonSerializer.Serialize(new { orderId = order.Id, companyId = company.Id, riderUserId = order.RiderUserId })
    };
    _db.Transactions.Add(tx);

    // Cập nhật trạng thái đơn
    order.Status = OrderStatus.Completed;
    order.UpdatedAt = DateTime.UtcNow;

    await _db.SaveChangesAsync();

    return ApiResponse<object>.Ok(new
    {
      orderId = order.Id,
      order.Status,
      transactionId = tx.Id,
      riderBalance = riderWallet.BalanceCents,
      companyBalance = companyWallet.BalanceCents
    });
  }


  // PUBLIC APIs

  [AllowAnonymous]
  [HttpGet("all-services")]
  [SwaggerOperation(Summary = "Public: List Services of all companies (with role-aware filters)")]
  [ProducesResponseType(typeof(ApiResponse<PageResult<Service>>), 200)]
  public async Task<ActionResult<ApiResponse<PageResult<Service>>>> ListServicesAllCompanies(
  [FromQuery] bool? isActive = true,                  // lọc service.IsActive (mặc định true)
  [FromQuery] string? q = null,
  [FromQuery] int page = 1,
  [FromQuery] int size = 10,
  [FromQuery] string? sort = null,                    // nếu null/empty => ưu tiên Premium→Basic→Free + random
  [FromQuery] bool? companyIsActive = null            // Admin có thể xem tất cả hoặc lọc; non-admin mặc định true
)
  {
    var baseQuery =
      from s in _db.Services
      join c in _db.Companies on s.CompanyId equals c.Id
      select new { S = s, C = c };

    // service.IsActive
    if (isActive.HasValue)
      baseQuery = baseQuery.Where(x => x.S.IsActive == isActive.Value);

    // search theo service
    if (!string.IsNullOrWhiteSpace(q))
      baseQuery = baseQuery.Where(x =>
        x.S.Title.Contains(q) ||
        (x.S.Description != null && x.S.Description.Contains(q)));

    // role-aware filter company.IsActive
    var isAdmin = User.IsInRole("Admin");
    if (isAdmin)
    {
      if (companyIsActive.HasValue)
        baseQuery = baseQuery.Where(x => x.C.IsActive == companyIsActive.Value);
    }
    else
    {
      var onlyActiveCompanies = companyIsActive ?? true; // mặc định true cho non-admin
      baseQuery = baseQuery.Where(x => x.C.IsActive == onlyActiveCompanies);
    }

    // gắn rank membership
    var ranked = baseQuery.Select(x => new
    {
      x.S,
      x.C,
      MembershipRank = x.C.Membership == "Premium" ? 3 :
                        x.C.Membership == "Basic" ? 2 : 1
    });

    // sort
    if (string.IsNullOrWhiteSpace(sort))
    {
      // DEFAULT: ưu tiên rank, random trong nhóm
      ranked = ranked
        .OrderByDescending(x => x.MembershipRank)
        .ThenBy(_ => EF.Functions.Random());
    }
    else
    {
      var s = sort.Split(':');
      var field = s[0];
      var dir = s.Length > 1 ? s[1] : "desc";

      ranked = (field, dir) switch
      {
        ("price", "asc") => ranked.OrderBy(x => x.S.PriceCents),
        ("price", "desc") => ranked.OrderByDescending(x => x.S.PriceCents),

        ("title", "asc") => ranked.OrderBy(x => x.S.Title),
        ("title", "desc") => ranked.OrderByDescending(x => x.S.Title),

        ("createdAt", "asc") => ranked.OrderBy(x => x.S.CreatedAt),
        ("createdAt", "desc") => ranked.OrderByDescending(x => x.S.CreatedAt),

        ("updatedAt", "asc") => ranked.OrderBy(x => x.S.UpdatedAt),
        _ => ranked.OrderByDescending(x => x.S.UpdatedAt),
      };
    }

    // chỉ còn Service để phân trang
    var qServices = ranked.Select(x => x.S);

    var total = await qServices.CountAsync();
    var items = await qServices
      .Skip((page - 1) * size)
      .Take(size)
      .ToListAsync();

    return ApiResponse<PageResult<Service>>.Ok(new PageResult<Service>
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
  

  [AllowAnonymous]
  [HttpGet("{companyId}/public")]
  [SwaggerOperation(Summary = "Public: Company profile with active services")]
  [ProducesResponseType(typeof(ApiResponse<CompanyPublicDto>), 200)]
  public async Task<ActionResult<ApiResponse<CompanyPublicDto>>> GetCompanyPublicProfile(
  [FromRoute] string companyId,
  [FromQuery] int page = 1,
  [FromQuery] int size = 6,
  [FromQuery] string? sort = "updatedAt:desc")
  {
    if (page <= 0) page = 1;
    if (size <= 0) size = 6;

    var company = await _db.Companies.AsNoTracking()
      .FirstOrDefaultAsync(c => c.Id == companyId);

    if (company == null)
      return ApiResponse<CompanyPublicDto>.Fail("NOT_FOUND", "Company không tồn tại");

    // query chỉ lấy services đang active của công ty
    IQueryable<Service> svcQuery = _db.Services.AsNoTracking()
      .Where(s => s.CompanyId == companyId && s.IsActive);

    // sort dịch vụ
    if (!string.IsNullOrWhiteSpace(sort))
    {
      var s = sort.Split(':');
      var field = s[0].Trim();
      var dir = (s.Length > 1 ? s[1] : "desc").Trim().ToLowerInvariant();

      svcQuery = (field, dir) switch
      {
        ("price", "asc") => svcQuery.OrderBy(sv => sv.PriceCents),
        ("price", "desc") => svcQuery.OrderByDescending(sv => sv.PriceCents),
        ("title", "asc") => svcQuery.OrderBy(sv => sv.Title),
        ("title", "desc") => svcQuery.OrderByDescending(sv => sv.Title),
        ("createdAt", "asc") => svcQuery.OrderBy(sv => sv.CreatedAt),
        ("createdAt", "desc") => svcQuery.OrderByDescending(sv => sv.CreatedAt),
        ("updatedAt", "asc") => svcQuery.OrderBy(sv => sv.UpdatedAt),
        _ => svcQuery.OrderByDescending(sv => sv.UpdatedAt)
      };
    }
    else
    {
      svcQuery = svcQuery.OrderByDescending(sv => sv.UpdatedAt);
    }

    var totalActive = await svcQuery.CountAsync();

    var services = await svcQuery
      .Skip((page - 1) * size)
      .Take(size)
      .ToListAsync();

    // số tài xế đã có quan hệ với công ty (không lọc active)
    var driversCount = await _db.CompanyDriverRelations
      .AsNoTracking()
      .CountAsync(r => r.CompanyId == companyId);

    var dto = new CompanyPublicDto
    {
      Company = company,
      Rating = company.Rating,
      ActiveServicesCount = totalActive,
      DriversCount = driversCount,
      Services = services,

      // metadata (FE có thể bỏ qua)
      Page = page,
      Size = size,
      TotalItems = totalActive,
      TotalPages = (int)Math.Ceiling(totalActive / (double)size)
    };

    return ApiResponse<CompanyPublicDto>.Ok(dto);
  }



}
