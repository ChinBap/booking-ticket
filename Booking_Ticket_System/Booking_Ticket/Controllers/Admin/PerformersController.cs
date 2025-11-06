using Booking_Ticket.Infrastructure.Querying;
using Booking_Ticket.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Booking_Ticket.Controllers.Admin;

[ApiController]
[Route("api/admin/performers")]
[Authorize(Roles = "Admin")]
public class PerformersController : ControllerBase
{
    private readonly EventBookingDbContext _db;
    public PerformersController(EventBookingDbContext db) => _db = db;

    // ✅ GET /api/admin/performers?q=&page=&pageSize=
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? q, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var query = _db.Performers.AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var k = q.Trim().ToLower();
            query = query.Where(p =>
                (p.StageName != null && p.StageName.ToLower().Contains(k)) ||
                (p.FullName != null && p.FullName.ToLower().Contains(k)));
        }

        var total = await query.CountAsync();
        var data = await query
            .OrderBy(p => p.StageName ?? p.FullName)
            .ApplyPaging(page, pageSize)
            .ToListAsync();

        return Ok(new { total, page, pageSize, data });
    }

    // ✅ GET /api/admin/performers/{id}
    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id)
    {
        var performer = await _db.Performers.FindAsync(id);
        if (performer == null) return NotFound();
        return Ok(performer);
    }

    // ✅ POST /api/admin/performers
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Performer model)
    {
        if (string.IsNullOrWhiteSpace(model.FullName) && string.IsNullOrWhiteSpace(model.StageName))
            return BadRequest(new { message = "Performer name is required" });

        _db.Performers.Add(model);
        await _db.SaveChangesAsync();

        return Ok(model);
    }

    // ✅ PUT /api/admin/performers/{id}
    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, [FromBody] Performer model)
    {
        var performer = await _db.Performers.FirstOrDefaultAsync(x => x.Id == id);
        if (performer == null) return NotFound();

        performer.StageName = model.StageName;
        performer.FullName = model.FullName;
        performer.AvatarUrl = model.AvatarUrl;
        performer.Bio = model.Bio;

        await _db.SaveChangesAsync();
        return Ok(performer);
    }

    // ✅ DELETE /api/admin/performers/{id}
    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id)
    {
        var performer = await _db.Performers.FirstOrDefaultAsync(x => x.Id == id);
        if (performer == null) return NotFound();

        _db.Performers.Remove(performer);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Deleted successfully" });
    }
}
