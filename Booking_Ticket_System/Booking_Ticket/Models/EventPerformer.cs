using System;
using System.Collections.Generic;

namespace Booking_Ticket.Models;

public partial class EventPerformer
{
    public long EventId { get; set; }

    public long PerformerId { get; set; }

    public string? RoleNote { get; set; }

    public virtual Event Event { get; set; } = null!;

    public virtual Performer Performer { get; set; } = null!;
}
