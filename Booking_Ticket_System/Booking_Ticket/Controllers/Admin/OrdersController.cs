using Booking_Ticket.Infrastructure.Querying;
using Booking_Ticket.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Booking_Ticket.Controllers.Admin;

[ApiController]
[Route("api/admin/orders")]
[Authorize(Roles = "Admin")]
public class OrdersController : ControllerBase
{
    private readonly EventBookingDbContext _db;
    public OrdersController(EventBookingDbContext db) => _db = db;

    // ✅ GET /api/admin/orders?q=&status=&paymentStatus=&page=&pageSize=
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? q,
        [FromQuery] string? status,
        [FromQuery] string? paymentStatus,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = _db.Orders
            .Include(o => o.User)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var k = q.Trim().ToLower();
            query = query.Where(o =>
                o.OrderCode.ToLower().Contains(k) ||
                (o.User != null && o.User.FullName.ToLower().Contains(k)));
        }

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(o => o.Status == status);

        if (!string.IsNullOrWhiteSpace(paymentStatus))
            query = query.Where(o => o.PaymentStatus == paymentStatus);

        var total = await query.CountAsync();
        var data = await query
            .OrderByDescending(o => o.CreatedAt)
            .ApplyPaging(page, pageSize)
            .Select(o => new
            {
                o.Id,
                o.OrderCode,
                UserName = o.User != null ? o.User.FullName : null,
                o.TotalAmount,
                o.Status,
                o.PaymentMethod,
                o.PaymentStatus,
                o.CreatedAt,
                o.PaidAt
            })
            .ToListAsync();

        return Ok(new { total, page, pageSize, data });
    }

    // ✅ GET /api/admin/orders/{id}
    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id)
    {
        var order = await _db.Orders
            .Include(o => o.User)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Event)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.TicketType)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null) return NotFound();
        return Ok(order);
    }

    // ✅ PUT /api/admin/orders/{id} (update trạng thái)
    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, [FromBody] Order model)
    {
        var order = await _db.Orders.FirstOrDefaultAsync(x => x.Id == id);
        if (order == null) return NotFound();

        order.Status = model.Status;
        order.PaymentStatus = model.PaymentStatus;
        order.PaymentMethod = model.PaymentMethod;
        order.Note = model.Note;
        order.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(order);
    }

    // ✅ DELETE /api/admin/orders/{id}
    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id)
    {
        var order = await _db.Orders.FirstOrDefaultAsync(x => x.Id == id);
        if (order == null) return NotFound();

        _db.Orders.Remove(order);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Deleted successfully" });
    }
}
