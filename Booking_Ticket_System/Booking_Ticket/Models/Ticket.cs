using System;
using System.Collections.Generic;

namespace Booking_Ticket.Models;

public partial class Ticket
{
    public long Id { get; set; }

    public string? TicketCode { get; set; }

    public long? OrderItemId { get; set; }

    public long? EventId { get; set; }

    public long? TicketTypeId { get; set; }

    public string? QrPayload { get; set; }

    public string? QrImageUrl { get; set; }

    public string? Status { get; set; }

    public DateTime? IssuedAt { get; set; }

    public DateTime? UsedAt { get; set; }

    public DateTime? CancelledAt { get; set; }

    public virtual Event? Event { get; set; }

    public virtual OrderItem? OrderItem { get; set; }

    public virtual ICollection<TicketScan> TicketScans { get; set; } = new List<TicketScan>();

    public virtual EventTicketType? TicketType { get; set; }
}
