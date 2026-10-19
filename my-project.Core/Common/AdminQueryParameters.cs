namespace my_project.Core.Common;

// SortBy: name (default), createdAt
public class AdminQueryParameters : PagingParameters
{
    // Users: FullName / Email. Families: Name / JoinCode
    public string? Search { get; set; }
}
