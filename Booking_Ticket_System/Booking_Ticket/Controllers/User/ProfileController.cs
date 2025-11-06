using System.Security.Cryptography;
using System.Text;
using Booking_Ticket.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Booking_Ticket.Controllers.User;

[ApiController]
[Route("api/profile")]
[Authorize(Roles = "User")]
public class ProfileController : ControllerBase
{
    private readonly EventBookingDbContext _db;
    public ProfileController(EventBookingDbContext db) => _db = db;

    // ✅ GET /api/profile
    [HttpGet]
    public async Task<IActionResult> GetProfile()
    {
        var userName = User.Identity?.Name;
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == userName);
        if (user == null) return Unauthorized();

        return Ok(new
        {
            user.Id,
            user.Username,
            user.FullName,
            user.Email,
            user.Phone,
            user.Gender,
            user.BirthDate,
            user.AvatarUrl,
            user.AddressLine,
            user.ProvinceName,
            user.DistrictName,
            user.WardName,
            user.CreatedAt
        });
    }

    // ✅ PUT /api/profile
    [HttpPut]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest req)
    {
        var userName = User.Identity?.Name;
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == userName);
        if (user == null) return Unauthorized();

        user.FullName = req.FullName ?? user.FullName;
        user.Email = req.Email ?? user.Email;
        user.Phone = req.Phone ?? user.Phone;
        user.Gender = req.Gender ?? user.Gender;

        if (req.BirthDate.HasValue)
            user.BirthDate = DateOnly.FromDateTime(req.BirthDate.Value);

        user.AddressLine = req.AddressLine ?? user.AddressLine;
        user.AvatarUrl = req.AvatarUrl ?? user.AvatarUrl;
        user.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(new { message = "Profile updated successfully" });
    }

    // ✅ PUT /api/profile/change-password
    [HttpPut("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest req)
    {
        var userName = User.Identity?.Name;
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == userName);
        if (user == null) return Unauthorized();

        string oldHash = ComputeSha256Hash(req.OldPassword);
        if (user.PasswordHash != oldHash)
            return BadRequest(new { message = "Old password is incorrect" });

        user.PasswordHash = ComputeSha256Hash(req.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(new { message = "Password changed successfully" });
    }

    // ✅ Helper SHA256
    private static string ComputeSha256Hash(string rawData)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawData));
        var sb = new StringBuilder();
        foreach (var b in bytes)
            sb.Append(b.ToString("x2"));
        return sb.ToString();
    }
}

// ✅ DTOs cho request
public class UpdateProfileRequest
{
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Gender { get; set; }
    public DateTime? BirthDate { get; set; }  // ✅ Giữ DateTime?, không đổi model
    public string? AddressLine { get; set; }
    public string? AvatarUrl { get; set; }
}

public class ChangePasswordRequest
{
    public string OldPassword { get; set; } = null!;
    public string NewPassword { get; set; } = null!;
}
