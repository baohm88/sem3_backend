using Api.Common;
using Api.Contracts.Riders;
using Domain.Entities;
using Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;

namespace Api.Controllers;

[ApiController]
[Route("api/riders")]
[SwaggerTag("Riders")]
public class RidersController : ControllerBase
{
    private readonly AppDbContext _db;
    public RidersController(AppDbContext db) => _db = db;

    // ===== Helpers =====

    /// <summary>Resolve current user id from JWT/claims.</summary>
    private string? GetUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier) ??
        User.FindFirstValue(ClaimTypes.Name) ??
        User.FindFirstValue("sub");

    /// <summary>Convenience Forbidden response.</summary>
    private ActionResult<ApiResponse<T>> Forbidden<T>() =>
        ApiResponse<T>.Fail("FORBIDDEN", "You don't have permission on this resource.");

    /// <summary>Get rider profile if it belongs to the current user; otherwise null.</summary>
    private async Task<RiderProfile?> GetOwnedRiderAsync(string riderUserId)
    {
        var uid = GetUserId();
        var r = await _db.RiderProfiles.FirstOrDefaultAsync(x => x.UserId == riderUserId);
        if (r == null) return null;
        return uid == riderUserId ? r : null;
    }

    private static string NewId() => Guid.NewGuid().ToString("N")[..24];

    // ========= Me =========

    [HttpGet("me")]
    [SwaggerOperation(Summary = "Get My Rider Profile (auto-create if missing)")]
    [ProducesResponseType(typeof(ApiResponse<RiderProfile>), 200)]
    public async Task<ActionResult<ApiResponse<RiderProfile>>> GetMyProfile()
    {
        var uid = GetUserId();
        if (uid == null) return ApiResponse<RiderProfile>.Fail("UNAUTHORIZED", "Missing user id");

        var p = await _db.RiderProfiles.FirstOrDefaultAsync(x => x.UserId == uid);
        if (p == null)
        {
            p = new RiderProfile
            {
                Id = NewId(),
                UserId = uid,
                FullName = "",
                Phone = null,
                ImgUrl = null,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _db.RiderProfiles.Add(p);
            await _db.SaveChangesAsync();
        }
        return ApiResponse<RiderProfile>.Ok(p);
    }

    [HttpPut("me")]
    [Consumes("application/json")]
    [SwaggerOperation(Summary = "Update My Rider Profile (upsert)")]
    [ProducesResponseType(typeof(ApiResponse<RiderProfile>), 200)]
    public async Task<ActionResult<ApiResponse<RiderProfile>>> UpdateMyProfile([FromBody] UpdateRiderDto dto)
    {
        var uid = GetUserId();
        if (uid == null) return ApiResponse<RiderProfile>.Fail("UNAUTHORIZED", "Missing user id");

        var p = await _db.RiderProfiles.FirstOrDefaultAsync(x => x.UserId == uid);
        if (p == null)
        {
            p = new RiderProfile
            {
                Id = NewId(),
                UserId = uid,
                FullName = dto.FullName ?? "",
                Phone = dto.Phone,
                ImgUrl = UrlHelper.TryNormalizeUrl(dto.ImgUrl),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _db.RiderProfiles.Add(p);
        }
        else
        {
            if (dto.FullName is not null) p.FullName = dto.FullName;
            if (dto.Phone is not null) p.Phone = dto.Phone;
            if (dto.ImgUrl is not null)
            {
                var normalized = UrlHelper.TryNormalizeUrl(dto.ImgUrl);
                if (normalized == null)
                    return ApiResponse<RiderProfile>.Fail("IMG_URL_INVALID", "Avatar is not a valid URL (http/https).");
                p.ImgUrl = normalized;
            }

            p.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        return ApiResponse<RiderProfile>.Ok(p);
    }

    // ========= Public listing & detail =========

    [HttpGet]
    [SwaggerOperation(Summary = "List Riders")]
    [ProducesResponseType(typeof(ApiResponse<PageResult<RiderProfile>>), 200)]
    public async Task<ActionResult<ApiResponse<PageResult<RiderProfile>>>> ListRiders(
        [FromQuery] string? name = null,
        [FromQuery] string? phone = null,
        [FromQuery] int page = 1, [FromQuery] int size = 10, [FromQuery] string? sort = "name:asc")
    {
        var q = _db.RiderProfiles.AsQueryable();
        if (!string.IsNullOrWhiteSpace(name)) q = q.Where(r => r.FullName.Contains(name));
        if (!string.IsNullOrWhiteSpace(phone)) q = q.Where(r => r.Phone != null && r.Phone.Contains(phone));

        if (!string.IsNullOrWhiteSpace(sort))
        {
            var s = sort.Split(':'); var field = s[0]; var dir = s.Length > 1 ? s[1] : "asc";
            q = (field, dir) switch
            {
                ("name", "desc") => q.OrderByDescending(r => r.FullName),
                ("name", "asc") => q.OrderBy(r => r.FullName),
                _ => q.OrderBy(r => r.FullName)
            };
        }

        var total = await q.CountAsync();
        var items = await q.Skip((page - 1) * size).Take(size).ToListAsync();
        return ApiResponse<PageResult<RiderProfile>>.Ok(new PageResult<RiderProfile>
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
    [SwaggerOperation(Summary = "Get Rider by UserId")]
    [ProducesResponseType(typeof(ApiResponse<RiderProfile>), 200)]
    public async Task<ActionResult<ApiResponse<RiderProfile>>> GetRiderByUserId([FromRoute] string userId)
    {
        var p = await _db.RiderProfiles.FirstOrDefaultAsync(x => x.UserId == userId);
        if (p == null) return ApiResponse<RiderProfile>.Fail("NOT_FOUND", "Rider does not exist.");
        return ApiResponse<RiderProfile>.Ok(p);
    }

    // ========= Wallet =========

    [Authorize(Roles = "Rider")]
    [HttpGet("{userId}/wallet")]
    [SwaggerOperation(Summary = "Get Rider Wallet")]
    [ProducesResponseType(typeof(ApiResponse<Wallet>), 200)]
    public async Task<ActionResult<ApiResponse<Wallet>>> GetWallet([FromRoute] string userId)
    {
        var p = await GetOwnedRiderAsync(userId);
        if (p == null) return Forbidden<Wallet>();

        var wallet = await _db.Wallets.FirstOrDefaultAsync(w => w.OwnerType == "Rider" && w.OwnerRefId == userId);
        if (wallet == null)
        {
            wallet = new Wallet
            {
                Id = NewId(),
                OwnerType = "Rider",
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

    [Authorize(Roles = "Rider")]
    [HttpGet("{userId}/transactions")]
    [SwaggerOperation(Summary = "List Rider Transactions")]
    [ProducesResponseType(typeof(ApiResponse<PageResult<Transaction>>), 200)]
    public async Task<ActionResult<ApiResponse<PageResult<Transaction>>>> GetTransactions(
        [FromRoute] string userId, [FromQuery] int page = 1, [FromQuery] int size = 10)
    {
        var p = await GetOwnedRiderAsync(userId);
        if (p == null) return Forbidden<PageResult<Transaction>>();

        var wallet = await _db.Wallets.FirstOrDefaultAsync(w => w.OwnerType == "Rider" && w.OwnerRefId == userId);
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

    [Authorize(Roles = "Rider")]
    [HttpPost("{userId}/wallet/topup")]
    [SwaggerOperation(Summary = "Topup Rider Wallet")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    public async Task<ActionResult<ApiResponse<object>>> Topup([FromRoute] string userId, [FromBody] TopupDto dto)
    {
        var p = await GetOwnedRiderAsync(userId);
        if (p == null) return Forbidden<object>();
        if (dto.AmountCents <= 0) return ApiResponse<object>.Fail("VALIDATION", "AmountCents must be > 0.");

        var wallet = await _db.Wallets.FirstOrDefaultAsync(w => w.OwnerType == "Rider" && w.OwnerRefId == userId);
        if (wallet == null)
        {
            wallet = new Wallet
            {
                Id = NewId(),
                OwnerType = "Rider",
                OwnerRefId = userId,
                BalanceCents = 0,
                LowBalanceThreshold = 10000,
                UpdatedAt = DateTime.UtcNow
            };
            _db.Wallets.Add(wallet);
        }

        // Idempotency for repeated topups
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
            CreatedAt = DateTime.UtcNow
        };
        _db.Transactions.Add(tx);

        await _db.SaveChangesAsync();
        return ApiResponse<object>.Ok(new { wallet.Id, balance = wallet.BalanceCents });
    }

    [Authorize(Roles = "Rider")]
    [HttpPost("{userId}/wallet/withdraw")]
    [SwaggerOperation(Summary = "Withdraw from Rider Wallet")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    public async Task<ActionResult<ApiResponse<object>>> Withdraw([FromRoute] string userId, [FromBody] WithdrawDto dto)
    {
        var p = await GetOwnedRiderAsync(userId);
        if (p == null) return Forbidden<object>();
        if (dto.AmountCents <= 0) return ApiResponse<object>.Fail("VALIDATION", "AmountCents must be > 0.");

        var wallet = await _db.Wallets.FirstOrDefaultAsync(w => w.OwnerType == "Rider" && w.OwnerRefId == userId);
        if (wallet == null || wallet.BalanceCents < dto.AmountCents)
            return ApiResponse<object>.Fail("INSUFFICIENT_FUNDS", "Insufficient balance.");

        // Idempotency for repeated withdrawals
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
            CreatedAt = DateTime.UtcNow
        };
        _db.Transactions.Add(tx);
        await _db.SaveChangesAsync();

        return ApiResponse<object>.Ok(new { wallet.Id, balance = wallet.BalanceCents });
    }

    // ========= Orders =========

    [Authorize(Roles = "Rider")]
    [HttpPost("{userId}/orders")]
    [SwaggerOperation(Summary = "Create Order")]
    [ProducesResponseType(typeof(ApiResponse<Order>), 200)]
    public async Task<ActionResult<ApiResponse<Order>>> CreateOrder([FromRoute] string userId, [FromBody] CreateOrderDto dto)
    {
        var p = await GetOwnedRiderAsync(userId);
        if (p == null) return Forbidden<Order>();
        if (string.IsNullOrWhiteSpace(dto.CompanyId)) return ApiResponse<Order>.Fail("VALIDATION", "CompanyId is required.");
        if (string.IsNullOrWhiteSpace(dto.ServiceId)) return ApiResponse<Order>.Fail("VALIDATION", "ServiceId is required.");

        var company = await _db.Companies.FirstOrDefaultAsync(c => c.Id == dto.CompanyId);
        if (company == null || !company.IsActive) return ApiResponse<Order>.Fail("NOT_FOUND", "Invalid company.");
        var svc = await _db.Services.FirstOrDefaultAsync(s => s.Id == dto.ServiceId && s.CompanyId == dto.CompanyId && s.IsActive);
        if (svc == null) return ApiResponse<Order>.Fail("NOT_FOUND", "Invalid service.");

        var order = new Order
        {
            Id = NewId(),
            RiderUserId = userId,
            CompanyId = dto.CompanyId!,
            ServiceId = dto.ServiceId!,
            Status = OrderStatus.Pending,
            PriceCents = svc.PriceCents,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.Orders.Add(order);
        await _db.SaveChangesAsync();
        return ApiResponse<Order>.Ok(order);
    }

    [Authorize(Roles = "Rider")]
    [HttpGet("{userId}/orders")]
    [SwaggerOperation(Summary = "List My Orders")]
    [ProducesResponseType(typeof(ApiResponse<PageResult<Order>>), 200)]
    public async Task<ActionResult<ApiResponse<PageResult<Order>>>> ListOrders(
        [FromRoute] string userId, [FromQuery] int page = 1, [FromQuery] int size = 10, [FromQuery] string? status = null)
    {
        var p = await GetOwnedRiderAsync(userId);
        if (p == null) return Forbidden<PageResult<Order>>();

        var q = _db.Orders.Where(o => o.RiderUserId == userId);
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<OrderStatus>(status, true, out var st))
            q = q.Where(o => o.Status == st);

        q = q.OrderByDescending(o => o.CreatedAt);
        var total = await q.CountAsync();
        var items = await q.Skip((page - 1) * size).Take(size).ToListAsync();
        return ApiResponse<PageResult<Order>>.Ok(new PageResult<Order>
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

    [Authorize(Roles = "Rider")]
    [HttpPost("{userId}/orders/{orderId}/cancel")]
    [SwaggerOperation(Summary = "Cancel Order")]
    [ProducesResponseType(typeof(ApiResponse<Order>), 200)]
    public async Task<ActionResult<ApiResponse<Order>>> CancelOrder([FromRoute] string userId, [FromRoute] string orderId)
    {
        var p = await GetOwnedRiderAsync(userId);
        if (p == null) return Forbidden<Order>();

        var o = await _db.Orders.FirstOrDefaultAsync(x => x.Id == orderId && x.RiderUserId == userId);
        if (o == null) return ApiResponse<Order>.Fail("NOT_FOUND", "Order does not exist.");
        if (o.Status == OrderStatus.Completed) return ApiResponse<Order>.Fail("INVALID_STATE", "Order already completed.");
        if (o.Status == OrderStatus.Cancelled) return ApiResponse<Order>.Fail("INVALID_STATE", "Order already cancelled.");

        o.Status = OrderStatus.Cancelled;
        o.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return ApiResponse<Order>.Ok(o);
    }

    // ========= Reviews =========

    [Authorize(Roles = "Rider")]
    [HttpPost("{userId}/orders/{orderId}/reviews")]
    [SwaggerOperation(Summary = "Review Order (Company)")]
    [ProducesResponseType(typeof(ApiResponse<Review>), 200)]
    public async Task<ActionResult<ApiResponse<Review>>> ReviewOrder([FromRoute] string userId, [FromRoute] string orderId, [FromBody] ReviewOrderDto dto)
    {
        var p = await GetOwnedRiderAsync(userId);
        if (p == null) return Forbidden<Review>();

        var o = await _db.Orders.FirstOrDefaultAsync(x => x.Id == orderId && x.RiderUserId == userId);
        if (o == null) return ApiResponse<Review>.Fail("NOT_FOUND", "Order does not exist.");
        if (o.Status != OrderStatus.Completed) return ApiResponse<Review>.Fail("INVALID_STATE", "You can only review a completed order.");

        var hasReview = await _db.Reviews.AnyAsync(r => r.OrderId == orderId && r.RiderUserId == userId);
        if (hasReview) return ApiResponse<Review>.Fail("DUPLICATE", "You have already reviewed this order.");

        var rv = new Review
        {
            Id = NewId(),
            OrderId = orderId,
            RiderUserId = userId,
            Rating = dto.Rating,
            Comment = dto.Comment,
            CreatedAt = DateTime.UtcNow
        };
        _db.Reviews.Add(rv);

        // Update company rating (simple average recalculation across its reviewed orders)
        var companyId = o.CompanyId;
        var allRatings = await _db.Reviews
            .Join(_db.Orders.Where(x => x.CompanyId == companyId),
                r => r.OrderId, o2 => o2.Id, (r, o2) => r.Rating)
            .ToListAsync();
        if (allRatings.Count > 0)
        {
            var avg = allRatings.Average();
            var company = await _db.Companies.FirstOrDefaultAsync(c => c.Id == companyId);
            if (company != null)
            {
                company.Rating = (decimal)avg;
                company.UpdatedAt = DateTime.UtcNow;
            }
        }

        await _db.SaveChangesAsync();
        return ApiResponse<Review>.Ok(rv);
    }

    [Authorize(Roles = "Rider")]
    [HttpGet("{userId}/reviews")]
    [SwaggerOperation(Summary = "List My Reviews")]
    [ProducesResponseType(typeof(ApiResponse<PageResult<Review>>), 200)]
    public async Task<ActionResult<ApiResponse<PageResult<Review>>>> ListMyReviews(
        [FromRoute] string userId, [FromQuery] int page = 1, [FromQuery] int size = 10)
    {
        var p = await GetOwnedRiderAsync(userId);
        if (p == null) return Forbidden<PageResult<Review>>();

        var q = _db.Reviews.Where(r => r.RiderUserId == userId).OrderByDescending(r => r.CreatedAt);
        var total = await q.CountAsync();
        var items = await q.Skip((page - 1) * size).Take(size).ToListAsync();
        return ApiResponse<PageResult<Review>>.Ok(new PageResult<Review>
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
}
