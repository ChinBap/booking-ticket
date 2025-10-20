using System;
using System.Collections.Generic;

namespace Booking_Ticket.Models;

public partial class OrderItem
{
    public long Id { get; set; }

    public long? OrderId { get; set; }

    public long? EventId { get; set; }

    public long? TicketTypeId { get; set; }

    public int? Quantity { get; set; }

    public decimal? UnitPrice { get; set; }

    public decimal? Subtotal { get; set; }

    public virtual Event? Event { get; set; }

    public virtual Order? Order { get; set; }

    public virtual EventTicketType? TicketType { get; set; }

    public virtual ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}
