using Booking_Ticket.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Booking_Ticket.Controllers.User;

[ApiController]
[Route("api/notifications")]
[Authorize(Roles = "User")]
public class NotificationsController : ControllerBase
{
    private readonly EventBookingDbContext _db;
    public NotificationsController(EventBookingDbContext db) => _db = db;

    // ✅ GET /api/notifications
    [HttpGet]
    public async Task<IActionResult> GetMyNotifications()
    {
        var userName = User.Identity?.Name;
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == userName);
        if (user == null) return Unauthorized();

        var notifications = await _db.Notifications
            .Where(n => n.UserId == user.Id)
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new
            {
                n.Id,
                n.Type,
                n.Title,
                n.Content,
                n.IsRead,
                n.CreatedAt
            })
            .ToListAsync();

        return Ok(notifications);
    }

    // ✅ PATCH /api/notifications/{id}/read
    [HttpPatch("{id:long}/read")]
    public async Task<IActionResult> MarkAsRead(long id)
    {
        var userName = User.Identity?.Name;
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == userName);
        if (user == null) return Unauthorized();

        var notification = await _db.Notifications
            .FirstOrDefaultAsync(n => n.Id == id && n.UserId == user.Id);
        if (notification == null)
            return NotFound(new { message = "Notification not found or not owned by user" });

        notification.IsRead = true;
        await _db.SaveChangesAsync();

        return Ok(new { message = "Marked as read successfully" });
    }
}
