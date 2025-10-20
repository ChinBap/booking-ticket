using System;
using System.Collections.Generic;

namespace Booking_Ticket.Models;

public partial class Performer
{
    public long Id { get; set; }

    public string? StageName { get; set; }

    public string? FullName { get; set; }

    public string? AvatarUrl { get; set; }

    public string? Bio { get; set; }

    public virtual ICollection<EventPerformer> EventPerformers { get; set; } = new List<EventPerformer>();
}
