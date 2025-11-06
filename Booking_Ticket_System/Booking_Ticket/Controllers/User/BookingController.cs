using Booking_Ticket.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Booking_Ticket.Controllers.User;

[ApiController]
[Route("api/booking")]
[Authorize(Roles = "User")]
public class BookingController : ControllerBase
{
    private readonly EventBookingDbContext _db;
    public BookingController(EventBookingDbContext db) => _db = db;

    // ✅ POST /api/booking  → Đặt vé (đã có)
    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest req)
    {
        var userName = User.Identity?.Name;
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == userName);
        if (user == null) return Unauthorized();

        var ticketType = await _db.EventTicketTypes.FirstOrDefaultAsync(t => t.Id == req.TicketTypeId);
        if (ticketType == null) return BadRequest(new { message = "Ticket type not found" });

        if (req.Quantity <= 0 || req.Quantity > ticketType.PerOrderLimit)
            return BadRequest(new { message = "Invalid quantity" });

        decimal subtotal = req.Quantity * (ticketType.Price ?? 0);

        var order = new Order
        {
            UserId = user.Id,
            OrderCode = $"ORD{DateTime.Now:yyyyMMddHHmmssfff}",
            Status = "Pending",
            PaymentMethod = req.PaymentMethod,
            PaymentStatus = "Unpaid",
            TotalAmount = subtotal,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Orders.Add(order);
        await _db.SaveChangesAsync();

        var item = new OrderItem
        {
            OrderId = order.Id,
            EventId = ticketType.EventId,
            TicketTypeId = ticketType.Id,
            Quantity = req.Quantity,
            UnitPrice = ticketType.Price ?? 0,
            Subtotal = subtotal
        };
        _db.OrderItems.Add(item);
        await _db.SaveChangesAsync();

        return Ok(new
        {
            message = "Order created successfully",
            order.OrderCode,
            order.TotalAmount,
            order.Status
        });
    }

    // ✅ NEW: GET /api/booking/my-orders  → xem các đơn hàng của user hiện tại
    [HttpGet("my-orders")]
    public async Task<IActionResult> GetMyOrders()
    {
        var userName = User.Identity?.Name;
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == userName);
        if (user == null) return Unauthorized();

        var orders = await _db.Orders
            .Where(o => o.UserId == user.Id)
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => new
            {
                o.Id,
                o.OrderCode,
                o.Status,
                o.PaymentStatus,
                o.PaymentMethod,
                o.TotalAmount,
                o.CreatedAt,
                o.UpdatedAt,
                Items = o.OrderItems.Select(i => new
                {
                    i.Id,
                    i.EventId,
                    EventName = i.Event != null ? i.Event.Name : null,
                    i.Quantity,
                    i.UnitPrice,
                    i.Subtotal
                })
            })
            .ToListAsync();

        return Ok(orders);
    }

    // ✅ PATCH /api/booking/{orderId}/cancel
    [HttpPatch("{orderId:long}/cancel")]
    public async Task<IActionResult> CancelOrder(long orderId)
    {
        var userName = User.Identity?.Name;
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == userName);
        if (user == null) return Unauthorized();

        var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == user.Id);
        if (order == null) return NotFound(new { message = "Order not found" });

        if (string.Equals(order.PaymentStatus, "Paid", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { message = "Cannot cancel a paid order" });

        if (string.Equals(order.Status, "Cancelled", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { message = "Order already cancelled" });

        order.Status = "Cancelled";
        order.CancelledAt = DateTime.UtcNow;
        order.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(new { message = "Order cancelled" });
    }
    // ✅ GET /api/booking/{orderId} → chi tiết đơn của user hiện tại
    [HttpGet("{orderId:long}")]
    public async Task<IActionResult> GetOrderDetail(long orderId)
    {
        var userName = User.Identity?.Name;
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == userName);
        if (user == null) return Unauthorized();

        var order = await _db.Orders
            .Where(o => o.Id == orderId && o.UserId == user.Id)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Event)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.TicketType)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Tickets)
            .FirstOrDefaultAsync();

        if (order == null)
            return NotFound(new { message = "Order not found" });

        // Lấy các giao dịch thanh toán liên quan
        var txs = await _db.PaymentTransactions
            .Where(t => t.OrderId == order.Id)
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new
            {
                t.Id,
                t.Provider,
                t.ProviderRef,
                t.Amount,
                t.Currency,
                t.Status,
                t.CreatedAt,
                t.UpdatedAt
            })
            .ToListAsync();

        var result = new
        {
            order.Id,
            order.OrderCode,
            order.Status,
            order.PaymentMethod,
            order.PaymentStatus,
            order.TotalAmount,
            order.Note,
            order.CreatedAt,
            order.UpdatedAt,
            order.PaidAt,
            order.CancelledAt,
            Items = order.OrderItems.Select(i => new
            {
                i.Id,
                i.EventId,
                EventName = i.Event != null ? i.Event.Name : null,
                EventLocation = i.Event != null ? i.Event.Location : null,
                EventStartTime = i.Event != null ? i.Event.StartTime : null,
                EventEndTime = i.Event != null ? i.Event.EndTime : null,
                i.TicketTypeId,
                TicketTypeName = i.TicketType != null ? i.TicketType.Name : null,
                i.UnitPrice,
                i.Quantity,
                i.Subtotal,
                Tickets = i.Tickets.Select(t => new
                {
                    t.Id,
                    t.TicketCode,
                    t.Status,
                    t.QrImageUrl,
                    t.IssuedAt,
                    t.UsedAt,
                    t.CancelledAt
                })
            }),
            Transactions = txs
        };

        return Ok(result);
    }


}

// ✅ Model cho request đặt vé
public class CreateOrderRequest
{
    public long TicketTypeId { get; set; }
    public int Quantity { get; set; }
    public string PaymentMethod { get; set; } = null!;
}
