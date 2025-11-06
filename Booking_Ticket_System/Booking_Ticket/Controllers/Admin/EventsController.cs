using Booking_Ticket.Infrastructure.Querying;
using Booking_Ticket.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Booking_Ticket.Controllers.Admin;

[ApiController]
[Route("api/admin/events")]
[Authorize(Roles = "Admin")]
public class EventsController : ControllerBase
{
    private readonly EventBookingDbContext _db;
    public EventsController(EventBookingDbContext db) => _db = db;

    // ✅ GET /api/admin/events?q=&categoryId=&from=&to=&page=&pageSize=
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? q,
        [FromQuery] long? categoryId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = _db.Events
            .Include(e => e.Category)
            .AsQueryable();

        // 🔍 Search theo tên, tiêu đề, mô tả
        if (!string.IsNullOrWhiteSpace(q))
        {
            var k = q.Trim().ToLower();
            query = query.Where(e =>
                e.Name.ToLower().Contains(k) ||
                (e.Title != null && e.Title.ToLower().Contains(k)) ||
                (e.Description != null && e.Description.ToLower().Contains(k)));
        }

        // 📂 Filter theo thể loại
        if (categoryId.HasValue)
            query = query.Where(e => e.CategoryId == categoryId);

        // 📅 Filter theo thời gian
        if (from.HasValue)
            query = query.Where(e => e.StartTime >= from.Value);
        if (to.HasValue)
            query = query.Where(e => e.EndTime <= to.Value);

        var total = await query.CountAsync();

        var data = await query
            .OrderByDescending(e => e.CreatedAt)
            .ApplyPaging(page, pageSize)
            .Select(e => new
            {
                e.Id,
                e.Title,
                e.Name,
                e.Location,
                e.StartTime,
                e.EndTime,
                e.BasePrice,
                e.IsHot,
                e.IsNew,
                e.Published,
                CategoryName = e.Category != null ? e.Category.Name : null
            })
            .ToListAsync();

        return Ok(new { total, page, pageSize, data });
    }

    // ✅ GET /api/admin/events/{id}
    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id)
    {
        var e = await _db.Events
            .Include(x => x.Category)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (e == null) return NotFound();

        return Ok(e);
    }

    // ✅ POST /api/admin/events
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Event model)
    {
        // ⚙️ Validate cơ bản
        if (string.IsNullOrWhiteSpace(model.Name))
            return BadRequest(new { message = "Event name is required" });

        model.CreatedAt = DateTime.UtcNow;
        model.UpdatedAt = DateTime.UtcNow;

        _db.Events.Add(model);
        await _db.SaveChangesAsync();

        return Ok(model);
    }

    // ✅ PUT /api/admin/events/{id}
    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, [FromBody] Event model)
    {
        var e = await _db.Events.FirstOrDefaultAsync(x => x.Id == id);
        if (e == null) return NotFound();

        // ⚙️ Cập nhật thủ công để tránh overposting
        e.Title = model.Title;
        e.Name = model.Name;
        e.Description = model.Description;
        e.Location = model.Location;
        e.StartTime = model.StartTime;
        e.EndTime = model.EndTime;
        e.BannerUrl = model.BannerUrl;
        e.CategoryId = model.CategoryId;
        e.BasePrice = model.BasePrice;
        e.IsHot = model.IsHot;
        e.IsNew = model.IsNew;
        e.Published = model.Published;
        e.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(e);
    }

    // ✅ DELETE /api/admin/events/{id}
    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id)
    {
        var e = await _db.Events.FirstOrDefaultAsync(x => x.Id == id);
        if (e == null) return NotFound();

        _db.Events.Remove(e);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Deleted successfully" });
    }
}
