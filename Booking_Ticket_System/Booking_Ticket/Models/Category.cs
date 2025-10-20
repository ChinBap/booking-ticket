using System;
using System.Collections.Generic;

namespace Booking_Ticket.Models;

public partial class Category
{
    public long Id { get; set; }

    public string? Name { get; set; }

    public string? Slug { get; set; }

    public string? Description { get; set; }

    public virtual ICollection<Event> Events { get; set; } = new List<Event>();
}
