using Booking_Ticket.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using UserEntity = Booking_Ticket.Models.User;

namespace Booking_Ticket.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly EventBookingDbContext _db;
    private readonly IConfiguration _config;
    public AuthController(EventBookingDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    // ✅ POST /api/auth/register
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] UserEntity model)
    {
        if (string.IsNullOrWhiteSpace(model.Username) || string.IsNullOrWhiteSpace(model.PasswordHash))
            return BadRequest(new { message = "Username and password are required" });

        bool exists = await _db.Users.AnyAsync(u => u.Username == model.Username);
        if (exists)
            return BadRequest(new { message = "Username already exists" });

        // 🔒 Hash password (simple SHA256 demo)
        model.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.PasswordHash);
        model.Role = "User";
        model.CreatedAt = DateTime.UtcNow;
        model.UpdatedAt = DateTime.UtcNow;

        _db.Users.Add(model);
        await _db.SaveChangesAsync();

        return Ok(new { message = "Registration successful" });
    }

    // ✅ POST /api/auth/login
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == req.Username);
        if (user == null || !BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
            return Unauthorized(new { message = "Invalid credentials" });

        var token = GenerateJwtToken(user);
        return Ok(new
        {
            token,
            user = new
            {
                user.Id,
                user.Username,
                user.FullName,
                user.Email,
                user.Role
            }
        });
    }

    // ✅ GET /api/auth/profile
    [HttpGet("profile")]
    [Authorize]
    public async Task<IActionResult> GetProfile()
    {
        var username = User.Identity?.Name;
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == username);
        if (user == null) return NotFound();
        return Ok(user);
    }

    // ✅ PUT /api/auth/profile
    [HttpPut("profile")]
    [Authorize]
    public async Task<IActionResult> UpdateProfile([FromBody] UserEntity model)
    {
        var username = User.Identity?.Name;
        var u = await _db.Users.FirstOrDefaultAsync(x => x.Username == username);
        if (u == null) return NotFound();

        u.FullName = model.FullName;
        u.Email = model.Email;
        u.Phone = model.Phone;
        u.AddressLine = model.AddressLine;
        u.ProvinceName = model.ProvinceName;
        u.DistrictName = model.DistrictName;
        u.WardName = model.WardName;
        u.Gender = model.Gender;
        u.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(u);
    }

    // ✅ PUT /api/auth/change-password
    [HttpPut("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest req)
    {
        var username = User.Identity?.Name;
        var u = await _db.Users.FirstOrDefaultAsync(x => x.Username == username);
        if (u == null) return NotFound();

        if (!BCrypt.Net.BCrypt.Verify(req.OldPassword, u.PasswordHash))
            return BadRequest(new { message = "Old password incorrect" });

        u.PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.NewPassword);
        u.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(new { message = "Password changed successfully" });
    }

    // 🔑 Tạo JWT
    private string GenerateJwtToken(UserEntity user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.Role ?? "User")
        };

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddDays(3),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

// 🧱 Request models
public class LoginRequest
{
    public string Username { get; set; } = null!;
    public string Password { get; set; } = null!;
}

public class ChangePasswordRequest
{
    public string OldPassword { get; set; } = null!;
    public string NewPassword { get; set; } = null!;
}
