namespace my_project.Core.DTOs.Users;

// A user as shown inside another object (organizer, author...)
public class UserSummaryDto
{
    public int Id { get; set; }

    public string FullName { get; set; } = string.Empty;
}
