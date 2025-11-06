using Booking_Ticket.Infrastructure.Querying;
using Booking_Ticket.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Booking_Ticket.Controllers.Admin;

[ApiController]
[Route("api/admin/notifications")]
[Authorize(Roles = "Admin")]
public class NotificationsController : ControllerBase
{
    private readonly EventBookingDbContext _db;
    public NotificationsController(EventBookingDbContext db) => _db = db;

    // ✅ GET /api/admin/notifications?q=&userId=&page=&pageSize=
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? q,
        [FromQuery] long? userId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = _db.Notifications.Include(n => n.User).AsQueryable();

        if (userId.HasValue)
            query = query.Where(n => n.UserId == userId);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var k = q.Trim().ToLower();
            query = query.Where(n =>
                n.Title.ToLower().Contains(k) ||
                (n.User != null && n.User.FullName.ToLower().Contains(k)));
        }

        var total = await query.CountAsync();
        var data = await query
            .OrderByDescending(n => n.CreatedAt)
            .ApplyPaging(page, pageSize)
            .Select(n => new
            {
                n.Id,
                n.Type,
                n.Title,
                n.IsRead,
                n.CreatedAt,
                UserName = n.User != null ? n.User.FullName : null
            })
            .ToListAsync();

        return Ok(new { total, page, pageSize, data });
    }

    // ✅ POST /api/admin/notifications
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Notification model)
    {
        model.CreatedAt = DateTime.UtcNow;
        _db.Notifications.Add(model);
        await _db.SaveChangesAsync();
        return Ok(model);
    }

    // ✅ PUT /api/admin/notifications/{id}
    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, [FromBody] Notification model)
    {
        var n = await _db.Notifications.FirstOrDefaultAsync(x => x.Id == id);
        if (n == null) return NotFound();

        n.Title = model.Title;
        n.Content = model.Content;
        n.IsRead = model.IsRead;
        n.Type = model.Type;
        await _db.SaveChangesAsync();

        return Ok(n);
    }

    // ✅ DELETE /api/admin/notifications/{id}
    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id)
    {
        var n = await _db.Notifications.FirstOrDefaultAsync(x => x.Id == id);
        if (n == null) return NotFound();

        _db.Notifications.Remove(n);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Deleted successfully" });
    }
}
