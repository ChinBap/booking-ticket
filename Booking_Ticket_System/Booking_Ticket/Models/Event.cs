using System;
using System.Collections.Generic;

namespace Booking_Ticket.Models;

public partial class Event
{
    public long Id { get; set; }

    public string? Title { get; set; }

    public string? Name { get; set; }

    public string? Description { get; set; }

    public string? Location { get; set; }

    public DateTime? StartTime { get; set; }

    public DateTime? EndTime { get; set; }

    public string? BannerUrl { get; set; }

    public long? CategoryId { get; set; }

    public decimal? BasePrice { get; set; }

    public bool? IsHot { get; set; }

    public bool? IsNew { get; set; }

    public bool? Published { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Category? Category { get; set; }

    public virtual ICollection<EventPerformer> EventPerformers { get; set; } = new List<EventPerformer>();

    public virtual ICollection<EventTicketType> EventTicketTypes { get; set; } = new List<EventTicketType>();

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}
