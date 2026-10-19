namespace my_project.Core.Common;

// Bound from the query string. Out-of-range values are clamped instead of rejected
public class PagingParameters
{
    public const int DefaultPageSize = 10;
    public const int MaxPageSize = 50;

    private int _page = 1;
    private int _pageSize = DefaultPageSize;

    public int Page
    {
        get => _page;
        set => _page = value < 1 ? 1 : value;
    }

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value < 1 ? DefaultPageSize : Math.Min(value, MaxPageSize);
    }

    // Matched against a closed list of fields in the repository, never used as a column name
    public string? SortBy { get; set; }

    public SortDirection SortDirection { get; set; } = SortDirection.Asc;
}
