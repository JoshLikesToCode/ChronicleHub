using System.Linq.Expressions;
using ChronicleHub.Domain.Entities;

namespace ChronicleHub.Api.ExtensionMethods;

public static class OrderByExtensions
{
    public static IQueryable<ActivityEvent> ApplySort(
        this IQueryable<ActivityEvent> source,
        string? sortBy,
        string? sortDirection)
    {
        if (string.IsNullOrWhiteSpace(sortBy))
        {
            return source.OrderByDescending(e => e.CreatedAtUtc);
        }

        var isDescending = sortDirection?.ToLowerInvariant() == "desc";

        return sortBy.ToLowerInvariant() switch
        {
            "createdat" or "createdatutc" => isDescending
                ? source.OrderByDescending(e => e.CreatedAtUtc)
                : source.OrderBy(e => e.CreatedAtUtc),

            "timestamp" or "timestamputc" => isDescending
                ? source.OrderByDescending(e => e.TimestampUtc)
                : source.OrderBy(e => e.TimestampUtc),

            "type" => isDescending
                ? source.OrderByDescending(e => e.Type)
                : source.OrderBy(e => e.Type),

            "source" => isDescending
                ? source.OrderByDescending(e => e.Source)
                : source.OrderBy(e => e.Source),

            _ => source.OrderByDescending(e => e.CreatedAtUtc) // Default fallback
        };
    }
}
