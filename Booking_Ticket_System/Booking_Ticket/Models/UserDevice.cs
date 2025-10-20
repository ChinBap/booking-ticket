using System;
using System.Collections.Generic;

namespace Booking_Ticket.Models;

public partial class UserDevice
{
    public long Id { get; set; }

    public long? UserId { get; set; }

    public string? FcmToken { get; set; }

    public string? DeviceInfo { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual User? User { get; set; }
}
