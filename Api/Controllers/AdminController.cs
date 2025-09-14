using Api.Common;
using Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Domain.Entities;

namespace Api.Controllers;

[ApiController]
[Route("api/admin")]
[SwaggerTag("Admin")]
public class AdminController : ControllerBase
{
  private readonly AppDbContext _db;
  public AdminController(AppDbContext db) => _db = db;
  // Compact DTO for user list rows (avoid exposing PasswordHash)
  public record AdminUserListItem(string Id, string Email, string Role, bool IsActive, DateTime CreatedAt);
  public record DeactivateDto(string ReasonCode, string? ReasonNote, DateTime? ExpiresAt);


  [Authorize(Roles = "Admin")]
  [HttpPost("users/{userId}/deactivate")]
  public async Task<ActionResult<ApiResponse<object>>> Deactivate(string userId, [FromBody] DeactivateDto dto)
  {

    var currentUserId = User?.FindFirst("sub")?.Value ?? User?.Identity?.Name;
    if (currentUserId == userId) return ApiResponse<object>.Fail("FORBIDDEN", "You cannot deactivate yourself.");

    var user = await _db.Users.FindAsync(userId);
    if (user == null) return ApiResponse<object>.Fail("NOT_FOUND", "User not found");
    user.IsActive = false;
    await _db.SaveChangesAsync();
    return ApiResponse<object>.Ok(new { user.Id, user.IsActive, dto.ReasonCode });
  }
  
  // ========= REACTIVATE USER =========
[Authorize(Roles = "Admin")]
[HttpPost("users/{userId}/reactivate")]
public async Task<ActionResult<ApiResponse<object>>> Reactivate(string userId, [FromBody] DeactivateDto? dto = null)
{
  var currentUserId = User?.FindFirst("sub")?.Value ?? User?.Identity?.Name;
  if (currentUserId == userId) return ApiResponse<object>.Fail("FORBIDDEN", "You cannot reactivate yourself.");

  var user = await _db.Users.FindAsync(userId);
  if (user == null) return ApiResponse<object>.Fail("NOT_FOUND", "User not found");
  user.IsActive = true;
  await _db.SaveChangesAsync();
  return ApiResponse<object>.Ok(new { user.Id, user.IsActive, ReasonCode = dto?.ReasonCode });
}

// ========= METRICS: TIME SERIES (day buckets) =========
[Authorize(Roles = "Admin")]
[HttpGet("metrics/timeseries")]
[SwaggerOperation(Summary = "Admin metrics time series (daily buckets)")]
public async Task<ActionResult<ApiResponse<object>>> GetMetricsTimeSeries(
  [FromQuery] DateTime? from = null,
  [FromQuery] DateTime? to = null)
{
  var end = (to ?? DateTime.UtcNow).Date;
  var start = (from ?? end.AddDays(-29)).Date; // default: last 30 days
  if (start > end) (start, end) = (end, start);

  var days = Enumerable.Range(0, (end - start).Days + 1)
                       .Select(i => start.AddDays(i))
                       .ToList();
  var dayLabels = days.Select(d => d.ToString("yyyy-MM-dd")).ToList();
  var startIncl = start;
  var endExcl   = end.AddDays(1);

  // --- New users per day
  var newUsers = await _db.Users.AsNoTracking()
    .Where(u => u.CreatedAt >= startIncl && u.CreatedAt < endExcl)
    .GroupBy(u => u.CreatedAt.Date)
    .Select(g => new { Date = g.Key, Count = g.Count() })
    .ToListAsync();
  var newUsersMap = newUsers.ToDictionary(x => x.Date, x => x.Count);

  // --- Completed orders per day + GMV
  var orders = await _db.Orders.AsNoTracking()
    .Where(o => o.Status == OrderStatus.Completed &&
                o.CreatedAt >= startIncl && o.CreatedAt < endExcl)
    .GroupBy(o => o.CreatedAt.Date)
    .Select(g => new
    {
      Date = g.Key,
      Count = g.Count(),
      Sum   = g.Sum(x => x.PriceCents)
    })
    .ToListAsync();
  var ordersCountMap = orders.ToDictionary(x => x.Date, x => x.Count);
  var ordersSumMap   = orders.ToDictionary(x => x.Date, x => x.Sum);

  // --- Membership revenue per day
  var membership = await _db.Transactions.AsNoTracking()
    .Where(t => t.Type == TxType.PayMembership &&
                t.Status == TxStatus.Completed &&
                t.CreatedAt >= startIncl && t.CreatedAt < endExcl)
    .GroupBy(t => t.CreatedAt.Date)
    .Select(g => new { Date = g.Key, Sum = g.Sum(x => x.AmountCents) })
    .ToListAsync();
  var membershipMap = membership.ToDictionary(x => x.Date, x => x.Sum);

  // Build aligned arrays (0 for missing days)
  var result = new
  {
    from = start,
    to = end,
    dates = dayLabels,
    newUsers = days.Select(d => newUsersMap.TryGetValue(d, out var v) ? v : 0).ToArray(),
    completedOrders = days.Select(d => ordersCountMap.TryGetValue(d, out var v) ? v : 0).ToArray(),
    orderGmvCents = days.Select(d => ordersSumMap.TryGetValue(d, out var v) ? v : 0).ToArray(),
    membershipRevenueCents = days.Select(d => membershipMap.TryGetValue(d, out var v) ? v : 0).ToArray()
  };

  return ApiResponse<object>.Ok(result);
}


 // ========= METRICS (sequential; DbContext-safe) =========
  [Authorize(Roles = "Admin")]
  [HttpGet("metrics")]
  [SwaggerOperation(Summary = "Admin metrics (date-range aware)")]
  public async Task<ActionResult<ApiResponse<object>>> GetMetrics(
    [FromQuery] DateTime? from = null,
    [FromQuery] DateTime? to = null)
  {
    try
    {
      IQueryable<Transaction> tq = _db.Transactions.AsNoTracking();
      if (from.HasValue) tq = tq.Where(t => t.CreatedAt >= from.Value);
      if (to.HasValue)   tq = tq.Where(t => t.CreatedAt <= to.Value);

      // counts (sequential)
      var totalUsers      = await _db.Users.AsNoTracking().CountAsync();
      var riders          = await _db.Users.AsNoTracking().CountAsync(u => u.Role == UserRole.Rider);
      var drivers         = await _db.Users.AsNoTracking().CountAsync(u => u.Role == UserRole.Driver);
      var companies       = await _db.Companies.AsNoTracking().CountAsync();
      var activeCompanies = await _db.Companies.AsNoTracking().CountAsync(c => c.IsActive);

      var transactionsCount = await tq.CountAsync();

      // helper: null-safe sum (returns 0 if no rows)
      static Task<long?> SumNullableAsync(IQueryable<Transaction> q) =>
        q.Where(x => x.Status == TxStatus.Completed)
        .SumAsync(x => (long?)x.AmountCents);

      // sums (sequential)
      var membershipRevenue = await SumNullableAsync(tq.Where(x => x.Type == TxType.PayMembership)) ?? 0;
      var orderGmv          = await SumNullableAsync(tq.Where(x => x.Type == TxType.OrderPayment)) ?? 0;
      var salaryPaid        = await SumNullableAsync(tq.Where(x => x.Type == TxType.PaySalary)) ?? 0;
      var topups            = await SumNullableAsync(tq.Where(x => x.Type == TxType.Topup)) ?? 0;
      var withdrawals       = await SumNullableAsync(tq.Where(x => x.Type == TxType.Withdraw)) ?? 0;

      return ApiResponse<object>.Ok(new
      {
        totalUsers,
        riders,
        drivers,
        companies,
        activeCompanies,

        transactionsCount,
        membershipRevenueCents = membershipRevenue,
        orderGmvCents          = orderGmv,
        salaryPaidCents        = salaryPaid,
        topupsCents            = topups,
        withdrawalsCents       = withdrawals
      });
    }
    catch (Exception ex)
    {
      // helpful error surface
      return ApiResponse<object>.Fail("INTERNAL_ERROR", ex.Message);
    }
  }


  // ========= TOP COMPANIES =========
  [Authorize(Roles = "Admin")]
  [HttpGet("top/companies")]
  [SwaggerOperation(Summary = "Top companies by completed order revenue")]
  public async Task<ActionResult<ApiResponse<object>>> TopCompanies([FromQuery] int limit = 5)
  {
    var q =
      from c in _db.Companies
      join o in _db.Orders.Where(o => o.Status == OrderStatus.Completed) on c.Id equals o.CompanyId into og
      select new
      {
        id = c.Id,
        name = c.Name,
        rating = c.Rating,
        revenueCents = og.Sum(x => (int?)x.PriceCents) ?? 0
      };

    var list = await q
      .OrderByDescending(x => x.revenueCents)
      .ThenByDescending(x => x.rating)
      .Take(Math.Max(1, limit))
      .ToListAsync();

    return ApiResponse<object>.Ok(list);
  }

  // ========= TOP DRIVERS =========
  [Authorize(Roles = "Admin")]
  [HttpGet("top/drivers")]
  [SwaggerOperation(Summary = "Top drivers by salary received")]
  public async Task<ActionResult<ApiResponse<object>>> TopDrivers([FromQuery] int limit = 5)
  {
    // PaySalary transactions set RefId = driverUserId (per your code)
    var salaryQ = _db.Transactions
      .Where(t => t.Type == TxType.PaySalary && t.Status == TxStatus.Completed);

    var q =
      from d in _db.DriverProfiles
      join t in salaryQ on d.UserId equals t.RefId into tg
      select new
      {
        userId = d.UserId,
        fullName = d.FullName,
        rating = d.Rating,
        salaryCents = tg.Sum(x => (int?)x.AmountCents) ?? 0
      };

    var list = await q
      .OrderByDescending(x => x.salaryCents)
      .ThenByDescending(x => x.rating)
      .Take(Math.Max(1, limit))
      .ToListAsync();

    return ApiResponse<object>.Ok(list);
  }

  // ========= USERS LIST (paged) =========
  [Authorize(Roles = "Admin")]
  [HttpGet("users")]
  [SwaggerOperation(Summary = "Paged users (filter by role & search by email)")]
  public async Task<ActionResult<ApiResponse<PageResult<AdminUserListItem>>>> ListUsers(
    [FromQuery] int page = 1,
    [FromQuery] int size = 10,
    [FromQuery] string? role = null,
    [FromQuery] string? search = null)
  {
    if (page < 1) page = 1;
    if (size < 1) size = 10;

    var q = _db.Users.AsQueryable();

    if (!string.IsNullOrWhiteSpace(role) &&
        Enum.TryParse<UserRole>(role, true, out var parsedRole))
    {
      q = q.Where(u => u.Role == parsedRole);
    }

    if (!string.IsNullOrWhiteSpace(search))
    {
      q = q.Where(u => u.Email.Contains(search));
    }

    q = q.OrderByDescending(u => u.CreatedAt);

    var total = await q.CountAsync();
    var items = await q
      .Skip((page - 1) * size)
      .Take(size)
      .Select(u => new AdminUserListItem(
        u.Id, u.Email, u.Role.ToString(), u.IsActive, u.CreatedAt))
      .ToListAsync();

    return ApiResponse<PageResult<AdminUserListItem>>.Ok(new PageResult<AdminUserListItem>
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