using Booking_Ticket.Infrastructure.Querying;
using Booking_Ticket.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Booking_Ticket.Controllers.User;

[ApiController]
[Route("api/events")]
public class EventsController : ControllerBase
{
    private readonly EventBookingDbContext _db;
    public EventsController(EventBookingDbContext db) => _db = db;

    // ✅ GET /api/events?hot=&new=&categoryId=&q=&page=&pageSize=
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> List(
        [FromQuery] bool? hot,
        [FromQuery] bool? @new,
        [FromQuery] long? categoryId,
        [FromQuery] string? q,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 12)
    {
        var query = _db.Events
            .Include(e => e.Category)
            .Where(e => e.Published == true);

        if (hot == true)
            query = query.Where(e => e.IsHot == true);

        if (@new == true)
            query = query.Where(e => e.IsNew == true);

        if (categoryId.HasValue)
            query = query.Where(e => e.CategoryId == categoryId.Value);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var k = q.Trim().ToLower();
            query = query.Where(e =>
                e.Name.ToLower().Contains(k) ||
                (e.Description != null && e.Description.ToLower().Contains(k)));
        }

        var total = await query.CountAsync();
        var data = await query
            .OrderByDescending(e => e.StartTime)
            .ApplyPaging(page, pageSize)
            .Select(e => new
            {
                e.Id,
                e.Title,
                e.Name,
                e.BannerUrl,
                e.Location,
                e.StartTime,
                e.EndTime,
                CategoryName = e.Category != null ? e.Category.Name : null
            })
            .ToListAsync();

        return Ok(new { total, page, pageSize, data });
    }

    // ✅ GET /api/events/{id}
    [HttpGet("{id:long}")]
    [AllowAnonymous]
    public async Task<IActionResult> Detail(long id)
    {
        var ev = await _db.Events
            .Include(e => e.Category)
            .FirstOrDefaultAsync(e => e.Id == id && e.Published == true);

        if (ev == null) return NotFound();

        var performers = await _db.EventPerformers
            .Where(ep => ep.EventId == id)
            .Include(ep => ep.Performer)
            .Select(ep => new
            {
                ep.PerformerId,
                ep.Performer.StageName,
                ep.Performer.FullName,
                ep.Performer.AvatarUrl,
                ep.RoleNote
            })
            .ToListAsync();

        var ticketTypes = await _db.EventTicketTypes
            .Where(t => t.EventId == id)
            .Select(t => new
            {
                t.Id,
                t.Name,
                t.Price,
                t.TotalQuantity,
                t.SoldQuantity,
                t.PerOrderLimit
            })
            .ToListAsync();

        return Ok(new
        {
            ev.Id,
            ev.Name,
            ev.Description,
            ev.BannerUrl,
            ev.Location,
            ev.StartTime,
            ev.EndTime,
            CategoryName = ev.Category?.Name,
            TicketTypes = ticketTypes,
            Performers = performers
        });
    }
}
