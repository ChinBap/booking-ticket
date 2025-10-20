using System;
using System.Collections.Generic;

namespace Booking_Ticket.Models;

public partial class EventTicketType
{
    public long Id { get; set; }

    public long EventId { get; set; }

    public string? Name { get; set; }

    public decimal? Price { get; set; }

    public int? TotalQuantity { get; set; }

    public int? SoldQuantity { get; set; }

    public int? PerOrderLimit { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Event Event { get; set; } = null!;

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}
