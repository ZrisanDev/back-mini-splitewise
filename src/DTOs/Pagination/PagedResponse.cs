namespace back_api_splitwise.src.DTOs.Pagination;

public record PagedResponse<T>(List<T> Items, int Page, int PageSize, int TotalCount, int TotalPages);
