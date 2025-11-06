using Booking_Ticket.Infrastructure.Querying;
using Booking_Ticket.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Booking_Ticket.Controllers.Admin;

[ApiController]
[Route("api/admin/tickets")]
[Authorize(Roles = "Admin")]
public class TicketsController : ControllerBase
{
    private readonly EventBookingDbContext _db;
    public TicketsController(EventBookingDbContext db) => _db = db;

    // ✅ GET /api/admin/tickets?q=&status=&page=&pageSize=
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? q,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = _db.Tickets
            .Include(t => t.Event)
            .Include(t => t.TicketType)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var k = q.Trim().ToLower();
            query = query.Where(t =>
                t.TicketCode.ToLower().Contains(k) ||
                (t.Event != null && t.Event.Name.ToLower().Contains(k)));
        }

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(t => t.Status == status);

        var total = await query.CountAsync();
        var data = await query
            .OrderByDescending(t => t.IssuedAt)
            .ApplyPaging(page, pageSize)
            .Select(t => new
            {
                t.Id,
                t.TicketCode,
                EventName = t.Event != null ? t.Event.Name : null,
                TicketType = t.TicketType != null ? t.TicketType.Name : null,
                t.Status,
                t.IssuedAt,
                t.UsedAt
            })
            .ToListAsync();

        return Ok(new { total, page, pageSize, data });
    }

    // ✅ GET /api/admin/tickets/{id}
    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id)
    {
        var ticket = await _db.Tickets
            .Include(t => t.Event)
            .Include(t => t.TicketType)
            .Include(t => t.OrderItem)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (ticket == null) return NotFound();
        return Ok(ticket);
    }

    // ✅ PUT /api/admin/tickets/{id}
    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, [FromBody] Ticket model)
    {
        var t = await _db.Tickets.FirstOrDefaultAsync(x => x.Id == id);
        if (t == null) return NotFound();

        t.Status = model.Status;
        t.UsedAt = model.UsedAt;
        t.CancelledAt = model.CancelledAt;
        await _db.SaveChangesAsync();

        return Ok(t);
    }

    // ✅ DELETE /api/admin/tickets/{id}
    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id)
    {
        var t = await _db.Tickets.FirstOrDefaultAsync(x => x.Id == id);
        if (t == null) return NotFound();

        _db.Tickets.Remove(t);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Deleted successfully" });
    }
}
