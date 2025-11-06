using Booking_Ticket.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Booking_Ticket.Controllers.User;

[ApiController]
[Route("api/tickets")]
[Authorize(Roles = "User")]
public class TicketsController : ControllerBase
{
    private readonly EventBookingDbContext _db;
    public TicketsController(EventBookingDbContext db) => _db = db;

    // ✅ GET /api/tickets/my-tickets
    [HttpGet("my-tickets")]
    public async Task<IActionResult> GetMyTickets()
    {
        var userName = User.Identity?.Name;
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == userName);
        if (user == null) return Unauthorized();

        // Lấy danh sách vé của user thông qua orders -> order_items -> tickets
        var tickets = await _db.Tickets
            .Include(t => t.Event)
            .Include(t => t.TicketType)
            .Include(t => t.OrderItem)
                .ThenInclude(oi => oi.Order)
            .Where(t => t.OrderItem.Order.UserId == user.Id)
            .OrderByDescending(t => t.IssuedAt)
            .Select(t => new
            {
                t.Id,
                t.TicketCode,
                EventName = t.Event != null ? t.Event.Name : null,
                TicketType = t.TicketType != null ? t.TicketType.Name : null,
                t.Status,
                t.QrImageUrl,
                t.IssuedAt,
                t.UsedAt,
                t.CancelledAt
            })
            .ToListAsync();

        return Ok(tickets);
    }

    // ✅ GET /api/tickets/{id}
    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetTicketDetail(long id)
    {
        var userName = User.Identity?.Name;
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == userName);
        if (user == null) return Unauthorized();

        var ticket = await _db.Tickets
            .Include(t => t.Event)
            .Include(t => t.TicketType)
            .Include(t => t.OrderItem)
                .ThenInclude(oi => oi.Order)
            .Where(t => t.Id == id && t.OrderItem.Order.UserId == user.Id)
            .Select(t => new
            {
                t.Id,
                t.TicketCode,
                EventName = t.Event != null ? t.Event.Name : null,
                EventLocation = t.Event != null ? t.Event.Location : null,
                EventTime = t.Event != null ? t.Event.StartTime : null,
                TicketType = t.TicketType != null ? t.TicketType.Name : null,
                t.QrPayload,
                t.QrImageUrl,
                t.Status,
                t.IssuedAt,
                t.UsedAt,
                t.CancelledAt
            })
            .FirstOrDefaultAsync();

        if (ticket == null)
            return NotFound(new { message = "Ticket not found or not owned by user" });

        return Ok(ticket);
    }
}
