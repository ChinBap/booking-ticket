using Booking_Ticket.Infrastructure.Querying;
using Booking_Ticket.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Booking_Ticket.Controllers.Admin;

[ApiController]
[Route("api/admin/ticket-types")]
[Authorize(Roles = "Admin")]
public class EventTicketTypesController : ControllerBase
{
    private readonly EventBookingDbContext _db;
    public EventTicketTypesController(EventBookingDbContext db) => _db = db;

    // ✅ GET /api/admin/ticket-types?eventId=&q=&page=&pageSize=
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] long? eventId,
        [FromQuery] string? q,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = _db.EventTicketTypes
            .Include(t => t.Event)
            .AsQueryable();

        if (eventId.HasValue)
            query = query.Where(t => t.EventId == eventId.Value);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var k = q.Trim().ToLower();
            query = query.Where(t => t.Name.ToLower().Contains(k));
        }

        var total = await query.CountAsync();

        var data = await query
            .OrderByDescending(t => t.CreatedAt)
            .ApplyPaging(page, pageSize)
            .Select(t => new
            {
                t.Id,
                t.Name,
                t.Price,
                t.TotalQuantity,
                t.SoldQuantity,
                t.PerOrderLimit,
                t.EventId,
                EventName = t.Event != null ? t.Event.Name : null
            })
            .ToListAsync();

        return Ok(new { total, page, pageSize, data });
    }

    // ✅ GET /api/admin/ticket-types/{id}
    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id)
    {
        var ticketType = await _db.EventTicketTypes
            .Include(t => t.Event)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (ticketType == null) return NotFound();
        return Ok(ticketType);
    }

    // ✅ POST /api/admin/ticket-types
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] EventTicketType model)
    {
        if (model.EventId <= 0)
            return BadRequest(new { message = "EventId is required" });

        if (string.IsNullOrWhiteSpace(model.Name))
            return BadRequest(new { message = "Ticket name is required" });

        model.CreatedAt = DateTime.UtcNow;
        model.UpdatedAt = DateTime.UtcNow;
        model.SoldQuantity ??= 0;

        _db.EventTicketTypes.Add(model);
        await _db.SaveChangesAsync();
        return Ok(model);
    }

    // ✅ PUT /api/admin/ticket-types/{id}
    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, [FromBody] EventTicketType model)
    {
        var t = await _db.EventTicketTypes.FirstOrDefaultAsync(x => x.Id == id);
        if (t == null) return NotFound();

        t.Name = model.Name;
        t.Price = model.Price;
        t.TotalQuantity = model.TotalQuantity;
        t.PerOrderLimit = model.PerOrderLimit;
        t.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(t);
    }

    // ✅ DELETE /api/admin/ticket-types/{id}
    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id)
    {
        var t = await _db.EventTicketTypes.FirstOrDefaultAsync(x => x.Id == id);
        if (t == null) return NotFound();

        _db.EventTicketTypes.Remove(t);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Deleted successfully" });
    }
}
