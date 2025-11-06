using System.Linq;

namespace Booking_Ticket.Infrastructure.Querying
{
    public static class QueryableExtensions
    {
        public static IQueryable<T> ApplyPaging<T>(this IQueryable<T> query, int page, int pageSize)
        {
            page = page <= 0 ? 1 : page;
            pageSize = pageSize <= 0 ? 20 : Math.Min(pageSize, 100);
            return query.Skip((page - 1) * pageSize).Take(pageSize);
        }
    }
}
