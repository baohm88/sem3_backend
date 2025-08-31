// using Api.Common;
// using Domain.Entities;
// using Infrastructure;
// using Microsoft.AspNetCore.Mvc;
// using Microsoft.EntityFrameworkCore;
// using Swashbuckle.AspNetCore.Annotations;
// using Api.Contracts.Companies;
// using Microsoft.AspNetCore.Authorization;
// using System.Security.Claims;

// namespace Api.Controllers;

// [ApiController]
// [Route("api/companies")]
// [SwaggerTag("Companies")]
// public class CompaniesController : ControllerBase
// {
//     private readonly AppDbContext _db;
//     public CompaniesController(AppDbContext db) => _db = db;

//     [HttpGet("drivers")]
//     public async Task<ActionResult<ApiResponse<PageResult<DriverProfile>>>> SearchDrivers(
//         [FromQuery] int page = 1, [FromQuery] int size = 10, [FromQuery] string? sort = null,
//         [FromQuery] decimal? rating = null, [FromQuery] string? skills = null, [FromQuery] string? location = null)
//     {
//         var query = _db.DriverProfiles.AsQueryable();
//         if (rating.HasValue) query = query.Where(d => d.Rating >= rating.Value);
//         if (!string.IsNullOrWhiteSpace(location)) query = query.Where(d => d.Location == location);
//         if (!string.IsNullOrWhiteSpace(skills))
//         {
//             var parts = skills.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
//             foreach (var p in parts)
//                 query = query.Where(d => d.Skills!.Contains(p));
//         }
//         if (!string.IsNullOrWhiteSpace(sort))
//         {
//             var s = sort.Split(':'); var field = s[0]; var dir = s.Length > 1 ? s[1] : "asc";
//             query = (field, dir) switch
//             {
//                 ("rating", "desc") => query.OrderByDescending(d => d.Rating),
//                 ("rating", "asc") => query.OrderBy(d => d.Rating),
//                 _ => query.OrderBy(d => d.FullName)
//             };
//         }
//         var total = await query.CountAsync();
//         var items = await query.Skip((page - 1) * size).Take(size).ToListAsync();
//         var result = new PageResult<DriverProfile>
//         {
//             page = page,
//             size = size,
//             totalItems = total,
//             totalPages = (int)Math.Ceiling(total / (double)size),
//             hasNext = page * size < total,
//             hasPrev = page > 1,
//             items = items
//         };
//         return ApiResponse<PageResult<DriverProfile>>.Ok(result);
//     }
// }
