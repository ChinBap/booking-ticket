using Booking_Ticket.Infrastructure.Querying;
using Booking_Ticket.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Booking_Ticket.Controllers.Admin;

[ApiController]
[Route("api/admin/event-performers")]
[Authorize(Roles = "Admin")]
public class EventPerformersController : ControllerBase
{
    private readonly EventBookingDbContext _db;
    public EventPerformersController(EventBookingDbContext db) => _db = db;

    // ✅ GET /api/admin/event-performers?eventId=&page=&pageSize=
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] long? eventId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = _db.EventPerformers
            .Include(ep => ep.Event)
            .Include(ep => ep.Performer)
            .AsQueryable();

        if (eventId.HasValue)
            query = query.Where(x => x.EventId == eventId.Value);

        var total = await query.CountAsync();

        var data = await query
            .OrderBy(x => x.Performer!.StageName ?? x.Performer!.FullName)
            .ApplyPaging(page, pageSize)
            .Select(x => new
            {
                x.EventId,
                EventName = x.Event != null ? x.Event.Name : null,
                x.PerformerId,
                PerformerName = x.Performer != null ? x.Performer.StageName ?? x.Performer.FullName : null,
                x.RoleNote
            })
            .ToListAsync();

        return Ok(new { total, page, pageSize, data });
    }

    // ✅ GET /api/admin/event-performers/{eventId}/{performerId}
    [HttpGet("{eventId:long}/{performerId:long}")]
    public async Task<IActionResult> GetById(long eventId, long performerId)
    {
        var ep = await _db.EventPerformers
            .Include(x => x.Event)
            .Include(x => x.Performer)
            .FirstOrDefaultAsync(x => x.EventId == eventId && x.PerformerId == performerId);

        if (ep == null) return NotFound();
        return Ok(ep);
    }

    // ✅ POST /api/admin/event-performers
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] EventPerformer model)
    {
        // Kiểm tra tồn tại
        var exists = await _db.EventPerformers.AnyAsync(x =>
            x.EventId == model.EventId && x.PerformerId == model.PerformerId);
        if (exists)
            return BadRequest(new { message = "Performer already added to this event" });

        // Kiểm tra event và performer có tồn tại không
        var evExists = await _db.Events.AnyAsync(e => e.Id == model.EventId);
        var pfExists = await _db.Performers.AnyAsync(p => p.Id == model.PerformerId);
        if (!evExists || !pfExists)
            return BadRequest(new { message = "Event or Performer not found" });

        _db.EventPerformers.Add(model);
        await _db.SaveChangesAsync();

        return Ok(model);
    }

    // ✅ PUT /api/admin/event-performers/{eventId}/{performerId}
    [HttpPut("{eventId:long}/{performerId:long}")]
    public async Task<IActionResult> Update(long eventId, long performerId, [FromBody] EventPerformer model)
    {
        var ep = await _db.EventPerformers
            .FirstOrDefaultAsync(x => x.EventId == eventId && x.PerformerId == performerId);
        if (ep == null) return NotFound();

        ep.RoleNote = model.RoleNote;
        await _db.SaveChangesAsync();

        return Ok(ep);
    }

    // ✅ DELETE /api/admin/event-performers/{eventId}/{performerId}
    [HttpDelete("{eventId:long}/{performerId:long}")]
    public async Task<IActionResult> Delete(long eventId, long performerId)
    {
        var ep = await _db.EventPerformers
            .FirstOrDefaultAsync(x => x.EventId == eventId && x.PerformerId == performerId);
        if (ep == null) return NotFound();

        _db.EventPerformers.Remove(ep);
        await _db.SaveChangesAsync();

        return Ok(new { message = "Deleted successfully" });
    }
}
