namespace Booking_Ticket.Infrastructure
{
    public static class DateExtensions
    {
        public static DateOnly ToDateOnly(this DateTime dt)
            => DateOnly.FromDateTime(dt);

        public static DateOnly? ToDateOnly(this DateTime? dt)
            => dt.HasValue ? DateOnly.FromDateTime(dt.Value) : null;

        public static DateTime ToDateTime(this DateOnly d, TimeOnly? time = null)
            => d.ToDateTime(time ?? TimeOnly.MinValue);

        public static DateTime? ToDateTime(this DateOnly? d, TimeOnly? time = null)
            => d.HasValue ? d.Value.ToDateTime(time ?? TimeOnly.MinValue) : null;
    }
}
