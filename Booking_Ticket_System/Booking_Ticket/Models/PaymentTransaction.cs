using System;
using System.Collections.Generic;

namespace Booking_Ticket.Models;

public partial class PaymentTransaction
{
    public long Id { get; set; }

    public long? OrderId { get; set; }

    public string? Provider { get; set; }

    public string? ProviderRef { get; set; }

    public decimal? Amount { get; set; }

    public string? Currency { get; set; }

    public string? Status { get; set; }

    public string? RawPayload { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Order? Order { get; set; }
}
