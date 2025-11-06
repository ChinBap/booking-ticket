using Booking_Ticket.Infrastructure.Querying;
using Booking_Ticket.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Booking_Ticket.Controllers.Admin;

[ApiController]
[Route("api/admin/categories")]
[Authorize(Roles = "Admin")]
public class CategoriesController : ControllerBase
{
    private readonly EventBookingDbContext _db;
    public CategoriesController(EventBookingDbContext db) => _db = db;

    // ✅ GET /api/admin/categories?q=&page=&pageSize=
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? q, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var query = _db.Categories.AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var k = q.Trim().ToLower();
            query = query.Where(c => c.Name.ToLower().Contains(k) ||
                                     (c.Slug != null && c.Slug.ToLower().Contains(k)));
        }

        var total = await query.CountAsync();
        var data = await query
            .OrderBy(c => c.Name)
            .ApplyPaging(page, pageSize)
            .ToListAsync();

        return Ok(new { total, page, pageSize, data });
    }

    // ✅ GET /api/admin/categories/{id}
    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id)
    {
        var cat = await _db.Categories.FindAsync(id);
        if (cat == null) return NotFound();
        return Ok(cat);
    }

    // ✅ POST /api/admin/categories
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Category model)
    {
        if (string.IsNullOrWhiteSpace(model.Name))
            return BadRequest(new { message = "Category name is required" });

        _db.Categories.Add(model);
        await _db.SaveChangesAsync();
        return Ok(model);
    }

    // ✅ PUT /api/admin/categories/{id}
    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, [FromBody] Category model)
    {
        var cat = await _db.Categories.FindAsync(id);
        if (cat == null) return NotFound();

        cat.Name = model.Name;
        cat.Slug = model.Slug;
        cat.Description = model.Description;

        await _db.SaveChangesAsync();
        return Ok(cat);
    }

    // ✅ DELETE /api/admin/categories/{id}
    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id)
    {
        var cat = await _db.Categories.FindAsync(id);
        if (cat == null) return NotFound();

        _db.Categories.Remove(cat);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Deleted successfully" });
    }
}
