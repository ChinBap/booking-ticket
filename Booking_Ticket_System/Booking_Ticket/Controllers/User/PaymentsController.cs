using Booking_Ticket.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Booking_Ticket.Controllers.User;

[ApiController]
[Route("api/payments")]
[Authorize(Roles = "User")]
public class PaymentsController : ControllerBase
{
    private readonly EventBookingDbContext _db;
    public PaymentsController(EventBookingDbContext db) => _db = db;

    // ✅ POST /api/payments/initiate
    // body: { "orderId": 123, "provider": "Momo" }
    // Tạo giao dịch thanh toán PENDING cho đơn thuộc user hiện tại
    [HttpPost("initiate")]
    public async Task<IActionResult> Initiate([FromBody] InitiatePaymentRequest req)
    {
        var userName = User.Identity?.Name;
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == userName);
        if (user == null) return Unauthorized();

        var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == req.OrderId && o.UserId == user.Id);
        if (order == null) return NotFound(new { message = "Order not found" });

        if (string.Equals(order.PaymentStatus, "Paid", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { message = "Order already paid" });

        // tạo transaction mới
        var providerRef = $"{req.Provider}-{DateTime.UtcNow:yyyyMMddHHmmssfff}-{order.Id}";
        var amount = order.TotalAmount ?? 0m;

        var tx = new PaymentTransaction
        {
            OrderId = order.Id,
            Provider = req.Provider,
            ProviderRef = providerRef,
            Amount = amount,
            Currency = "VND",
            Status = "Pending",
            RawPayload = null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.PaymentTransactions.Add(tx);
        await _db.SaveChangesAsync();

        // Trả về URL mô phỏng redirect (client tự mở)
        var fakeRedirectUrl = $"https://sandbox-pay.example.com/{req.Provider}/pay?ref={Uri.EscapeDataString(providerRef)}&amount={(long)amount}";
        return Ok(new
        {
            message = "Payment initiated",
            provider = req.Provider,
            providerRef,
            amount,
            redirectUrl = fakeRedirectUrl
        });
    }

    // ✅ POST /api/payments/callback
    // body: { "providerRef":"Momo-...", "status":"Success|Failed", "amount":100000, "rawPayload":"..." }
    // Mô phỏng callback từ cổng thanh toán → cập nhật transaction + order
    [HttpPost("callback")]
    [AllowAnonymous] // callback thường không kèm JWT; nếu muốn bảo vệ thì thêm secret header
    public async Task<IActionResult> Callback([FromBody] PaymentCallbackRequest req)
    {
        var tx = await _db.PaymentTransactions
            .Include(t => t.Order)
            .FirstOrDefaultAsync(t => t.ProviderRef == req.ProviderRef);

        if (tx == null) return NotFound(new { message = "Transaction not found" });

        // cập nhật transaction
        tx.Status = req.Status;
        tx.RawPayload = req.RawPayload;
        tx.UpdatedAt = DateTime.UtcNow;

        // cập nhật order nếu thành công
        if (string.Equals(req.Status, "Success", StringComparison.OrdinalIgnoreCase))
        {
            if (tx.Order != null)
            {
                tx.Order.PaymentStatus = "Paid";
                tx.Order.Status = "Paid";
                tx.Order.PaidAt = DateTime.UtcNow;
                tx.Order.UpdatedAt = DateTime.UtcNow;
            }
        }

        await _db.SaveChangesAsync();

        return Ok(new
        {
            message = "Payment callback processed",
            providerRef = req.ProviderRef,
            status = req.Status
        });
    }

    // ✅ GET /api/payments/my
    // Liệt kê các giao dịch thanh toán của user hiện tại
    [HttpGet("my")]
    public async Task<IActionResult> MyPayments()
    {
        var userName = User.Identity?.Name;
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == userName);
        if (user == null) return Unauthorized();

        var data = await _db.PaymentTransactions
            .Include(t => t.Order)
            .Where(t => t.Order != null && t.Order.UserId == user.Id)
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
                OrderCode = t.Order!.OrderCode
            })
            .ToListAsync();

        return Ok(data);
    }
}

// ✅ Request models
public class InitiatePaymentRequest
{
    public long OrderId { get; set; }
    public string Provider { get; set; } = null!;
}

public class PaymentCallbackRequest
{
    public string ProviderRef { get; set; } = null!;
    public string Status { get; set; } = null!; // "Success" | "Failed"
    public decimal Amount { get; set; }
    public string? RawPayload { get; set; }
}
