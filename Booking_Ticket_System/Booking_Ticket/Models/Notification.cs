using System;
using System.Collections.Generic;

namespace Booking_Ticket.Models;

public partial class Notification
{
    public long Id { get; set; }

    public long? UserId { get; set; }

    public string? Type { get; set; }

    public string? Title { get; set; }

    public string? Content { get; set; }

    public bool? IsRead { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual User? User { get; set; }
}
