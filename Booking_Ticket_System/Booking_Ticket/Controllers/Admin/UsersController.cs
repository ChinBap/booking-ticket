using Booking_Ticket.Infrastructure.Querying;
using Booking_Ticket.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UserEntity = Booking_Ticket.Models.User;

namespace Booking_Ticket.Controllers.Admin;

[ApiController]
[Route("api/admin/users")]
[Authorize(Roles = "Admin")]
public class UsersController : ControllerBase
{

    private readonly EventBookingDbContext _db;
    public UsersController(EventBookingDbContext db) => _db = db;

    // ✅ GET /api/admin/users?q=&role=&page=&pageSize=
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? q,
        [FromQuery] string? role,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = _db.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var k = q.Trim().ToLower();
            query = query.Where(u =>
                u.Username.ToLower().Contains(k) ||
                u.FullName.ToLower().Contains(k) ||
                (u.Email != null && u.Email.ToLower().Contains(k)));
        }

        if (!string.IsNullOrWhiteSpace(role))
            query = query.Where(u => u.Role == role);

        var total = await query.CountAsync();
        var data = await query
            .OrderByDescending(u => u.CreatedAt)
            .ApplyPaging(page, pageSize)
            .Select(u => new
            {
                u.Id,
                u.Username,
                u.FullName,
                u.Email,
                u.Phone,
                u.Role,
                u.CreatedAt
            })
            .ToListAsync();

        return Ok(new { total, page, pageSize, data });
    }

    // ✅ GET /api/admin/users/{id}
    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id)
    {
        var u = await _db.Users.FirstOrDefaultAsync(x => x.Id == id);
        if (u == null) return NotFound();
        return Ok(u);
    }

    // ✅ POST /api/admin/users
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] UserEntity model)
    {
        if (string.IsNullOrWhiteSpace(model.Username))
            return BadRequest(new { message = "Username required" });

        model.CreatedAt = DateTime.UtcNow;
        model.UpdatedAt = DateTime.UtcNow;
        _db.Users.Add(model);
        await _db.SaveChangesAsync();
        return Ok(model);
    }

    // ✅ PUT /api/admin/users/{id}
    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, [FromBody] UserEntity model)
    {
        var u = await _db.Users.FirstOrDefaultAsync(x => x.Id == id);
        if (u == null) return NotFound();

        u.FullName = model.FullName;
        u.Email = model.Email;
        u.Phone = model.Phone;
        u.Role = model.Role;
        u.Gender = model.Gender;
        u.AddressLine = model.AddressLine;
        u.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(u);
    }

    // ✅ DELETE /api/admin/users/{id}
    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id)
    {
        var u = await _db.Users.FirstOrDefaultAsync(x => x.Id == id);
        if (u == null) return NotFound();

        _db.Users.Remove(u);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Deleted successfully" });
    }
}
