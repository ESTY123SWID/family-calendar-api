namespace my_project.Core.DTOs.Admin;

public class AdminStatsDto
{
    public int TotalUsers { get; set; }

    public int BlockedUsers { get; set; }

    public int TotalFamilies { get; set; }

    public int TotalEvents { get; set; }

    // Scheduled events that have not started yet
    public int UpcomingEvents { get; set; }

    public int TotalTasks { get; set; }

    // Tasks that are not Done
    public int OpenTasks { get; set; }
}
