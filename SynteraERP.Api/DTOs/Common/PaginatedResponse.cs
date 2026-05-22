namespace SynteraERP.Api.DTOs.Common;

public class PaginatedResponse<T>
{
    public IReadOnlyList<T> Data { get; set; } = [];
    public int Total { get; set; }
    public int Page { get; set; }
    public int PerPage { get; set; }
    public int TotalPages => PerPage > 0 ? (int)Math.Ceiling((double)Total / PerPage) : 0;

    public static PaginatedResponse<T> Create(IReadOnlyList<T> data, int total, int page, int perPage) =>
        new() { Data = data, Total = total, Page = page, PerPage = perPage };
}

public class PaginationParams
{
    private int _perPage = 10;

    public int Page { get; set; } = 1;
    public int PerPage
    {
        get => _perPage;
        set => _perPage = Math.Clamp(value, 1, 100);
    }
    public string? Search { get; set; }
    public string? SortBy { get; set; }
    public string? SortDir { get; set; } = "asc";

    public int Skip => (Page - 1) * PerPage;
    public bool IsDescending => SortDir?.ToLower() == "desc";
}
