using Booking_Ticket.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Booking_Ticket.Controllers.Auth
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly EventBookingDbContext _context;

        public AuthController(EventBookingDbContext eventBookingDbContext)
        {
            _context = eventBookingDbContext;
        }

        [HttpGet("Login")]
        public IActionResult Login()
        {
            return Ok();
        }

    }
}
