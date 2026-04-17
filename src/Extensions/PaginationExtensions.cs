using back_api_splitwise.src.DTOs.Pagination;

namespace back_api_splitwise.src.Extensions;

public static class PaginationExtensions
{
    public static IQueryable<T> Paginate<T>(this IQueryable<T> query, int page, int pageSize)
    {
        var validatedPage = Math.Max(1, page);
        var validatedPageSize = Math.Max(1, Math.Min(pageSize, 100));

        return query
            .Skip((validatedPage - 1) * validatedPageSize)
            .Take(validatedPageSize);
    }

    public static PagedResponse<T> ToPagedResponse<T>(
        this List<T> items,
        int page,
        int pageSize,
        int totalCount) where T : class
    {
        var validatedPage = Math.Max(1, page);
        var validatedPageSize = Math.Max(1, Math.Min(pageSize, 100));
        var totalPages = (int)Math.Ceiling((double)totalCount / validatedPageSize);

        return new PagedResponse<T>(items, validatedPage, validatedPageSize, totalCount, totalPages);
    }
}
