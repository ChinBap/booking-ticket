using System;
using System.Collections.Generic;

namespace Booking_Ticket.Models;

public partial class TicketScan
{
    public long Id { get; set; }

    public long? TicketId { get; set; }

    public DateTime? ScannedAt { get; set; }

    public string? Gate { get; set; }

    public string? DeviceId { get; set; }

    public string? Result { get; set; }

    public virtual Ticket? Ticket { get; set; }
}
