namespace my_project.Core.Entities;

public class Family
{
    public int Id { get; set; }

    // Required, max 100
    public string Name { get; set; } = string.Empty;

    // Required, max 20, unique. Generated automatically, e.g. LEVI-4821
    public string JoinCode { get; set; } = string.Empty;

    // FK → User (Restrict)
    public int CreatedById { get; set; }

    // UTC
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User CreatedBy { get; set; } = null!;
    public ICollection<FamilyMember> Members { get; set; } = new List<FamilyMember>();
    public ICollection<Event> Events { get; set; } = new List<Event>();
}
