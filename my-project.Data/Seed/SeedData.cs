using Microsoft.EntityFrameworkCore;
using my_project.Core.Entities;
using my_project.Core.Enums;

namespace my_project.Data.Seed;

// Demo data, applied by the "SeedData" migration (HasData), so a new database can be used at once.
//
// Rules of HasData: every value is a constant – no Guid.NewGuid(), no DateTime.UtcNow, and the
// password is stored only as a pre-computed hash. Otherwise every new migration would "change" the seed.
//
// All demo users share the demo password "Demo1234!" (documented in the README; the hash below was
// produced once with PasswordHasher). The system admin is NOT here: AdminSeeder creates it at startup
// from configuration (User Secrets / environment variables), so no admin password is ever in the repo.
public static class SeedData
{
    private const string DemoPasswordHash = "AQAAAAIAAYagAAAAEMkwev6h9CPGAXrtMAsHJNPVkFlvphwSyc+MylHnzACrYX9x/hUVUvk0S4sPDRzKiQ==";

    // Fixed points in time (UTC). The events are in 2027, so they are upcoming when the project is checked
    private static readonly DateTime Created = new(2026, 9, 1, 8, 0, 0, DateTimeKind.Utc);
    private static DateTime At(int year, int month, int day, int hour, int minute = 0) => new(year, month, day, hour, minute, 0, DateTimeKind.Utc);

    public static void Apply(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>().HasData(
            User(1, "שרה לוי", "sara@family.demo"),
            User(2, "דוד לוי", "david@family.demo"),
            User(3, "משה לוי", "moshe@family.demo"),
            User(4, "רבקה כהן", "rivka@family.demo"),
            User(5, "נועה לוי", "noa@family.demo"),
            User(6, "יעל פרידמן", "yael@family.demo"));

        modelBuilder.Entity<NotificationSettings>().HasData(
            Enumerable.Range(1, 6).Select(userId => new NotificationSettings { UserId = userId }));

        modelBuilder.Entity<Family>().HasData(
            new Family { Id = 1, Name = "משפחת לוי", JoinCode = "LEVI-4821", CreatedById = 1, CreatedAt = Created },
            new Family { Id = 2, Name = "משפחת כהן", JoinCode = "COHEN-3307", CreatedById = 4, CreatedAt = Created });

        // David (2) is in both families
        modelBuilder.Entity<FamilyMember>().HasData(
            Member(1, 1, FamilyRole.Manager),
            Member(1, 2, FamilyRole.Member),
            Member(1, 3, FamilyRole.Member),
            Member(1, 5, FamilyRole.Member),
            Member(1, 6, FamilyRole.Member),
            Member(2, 4, FamilyRole.Manager),
            Member(2, 2, FamilyRole.Member));

        modelBuilder.Entity<Event>().HasData(
            new Event
            {
                Id = 1, FamilyId = 1, OrganizerId = 1, Title = "שבת משפחתית אצל סבא וסבתא",
                Description = "שבת של כל המשפחה יחד. מביאים מצב רוח טוב – את השאר מחלקים במשימות.",
                StartAt = At(2027, 1, 15, 14, 45), EndAt = At(2027, 1, 16, 18, 0),
                LocationName = "הבית של סבא וסבתא", Address = "רחוב הזית 12, ירושלים",
                Directions = "חניה בחניון הציבורי ברחוב הסמוך. כניסה ב׳, קומה 2.",
                Status = EventStatus.Scheduled, CreatedAt = Created, Version = new Guid("a1000000-0000-0000-0000-000000000001"),
            },
            new Event
            {
                Id = 2, FamilyId = 1, OrganizerId = 2, Title = "יום הולדת 80 לסבתא רחל",
                Description = "ערב הפתעה – לא לספר לסבתא!",
                StartAt = At(2027, 2, 3, 17, 30), EndAt = At(2027, 2, 3, 21, 0),
                LocationName = "אולם הגפן", Address = "רחוב ההגנה 5, פתח תקווה", Directions = "חניה חופשית מאחורי האולם.",
                Status = EventStatus.Scheduled, CreatedAt = Created, Version = new Guid("a1000000-0000-0000-0000-000000000002"),
            },
            new Event
            {
                Id = 3, FamilyId = 1, OrganizerId = 1, Title = "מסיבת פורים לילדים",
                StartAt = At(2027, 3, 23, 15, 0), LocationName = "המתנ״ס השכונתי", Address = "רחוב הדקל 3, ירושלים",
                Status = EventStatus.Cancelled, CreatedAt = Created, Version = new Guid("a1000000-0000-0000-0000-000000000003"),
            },
            new Event
            {
                Id = 4, FamilyId = 2, OrganizerId = 4, Title = "ארוחת שישי אצל רבקה",
                Description = "ארוחה חגיגית לכבוד המעבר לדירה החדשה.",
                StartAt = At(2027, 1, 22, 17, 0), LocationName = "הדירה החדשה", Address = "שדרות רוטשילד 40, תל אביב",
                Directions = "קומה 4, יש מעלית.",
                Status = EventStatus.Scheduled, CreatedAt = Created, Version = new Guid("a1000000-0000-0000-0000-000000000004"),
            });

        modelBuilder.Entity<EventParticipant>().HasData(
            Participant(1, 1, RsvpStatus.Coming, 3),
            Participant(1, 2, RsvpStatus.Coming, 4),
            Participant(1, 3, RsvpStatus.Coming, 1),
            Participant(1, 5, RsvpStatus.Maybe, 2),
            Participant(1, 6, RsvpStatus.Pending),
            Participant(2, 2, RsvpStatus.Coming, 2),
            Participant(2, 1, RsvpStatus.Coming, 1),
            Participant(2, 3, RsvpStatus.Pending),
            Participant(2, 5, RsvpStatus.NotComing),
            Participant(2, 6, RsvpStatus.Pending),
            Participant(3, 1, RsvpStatus.Coming),
            Participant(3, 2, RsvpStatus.Pending),
            Participant(4, 4, RsvpStatus.Coming, 1),
            Participant(4, 2, RsvpStatus.Pending));

        // The limited resource: open tasks (AssigneeId = null) that members volunteer for
        modelBuilder.Entity<FamilyTask>().HasData(
            Task(1, 1, "להזמין עוגות לקינוח", TaskCategory.Food, TaskPriority.Urgent, FamilyTaskStatus.InProgress, At(2027, 1, 13, 10), 2),
            Task(2, 1, "להביא מיחם ופלטה", TaskCategory.Equipment, TaskPriority.High, FamilyTaskStatus.Open, At(2027, 1, 14, 16), null),
            Task(3, 1, "קניית כלים חד־פעמיים ומפות", TaskCategory.Shopping, TaskPriority.Medium, FamilyTaskStatus.Open, At(2027, 1, 14, 18), null),
            Task(4, 1, "לסדר שולחנות וכיסאות", TaskCategory.Venue, TaskPriority.Medium, FamilyTaskStatus.Open, At(2027, 1, 15, 12), null),
            Task(5, 1, "להכין סלטים", TaskCategory.Food, TaskPriority.High, FamilyTaskStatus.Assigned, At(2027, 1, 15, 10), 5),
            Task(6, 1, "להביא כיסאות מתקפלים", TaskCategory.Equipment, TaskPriority.Medium, FamilyTaskStatus.Done, At(2027, 1, 14, 10), 3),
            Task(7, 2, "להזמין את העוגה", TaskCategory.Food, TaskPriority.High, FamilyTaskStatus.Assigned, At(2027, 1, 31, 10), 2),
            Task(8, 2, "מצגת תמונות של סבתא", TaskCategory.Preparation, TaskPriority.Medium, FamilyTaskStatus.Open, At(2027, 2, 1, 18), null),
            Task(9, 4, "להביא יין", TaskCategory.Shopping, TaskPriority.Medium, FamilyTaskStatus.Open, At(2027, 1, 21, 16), null));

        modelBuilder.Entity<EventUpdate>().HasData(
            new EventUpdate { Id = 1, EventId = 1, AuthorId = 1, Content = "שעת הכניסה הוקדמה ל־16:45. מי שמביא אוכל – עד 15:30, בבקשה.", CreatedAt = At(2026, 10, 1, 9) },
            new EventUpdate { Id = 2, EventId = 1, AuthorId = 1, Content = "שמרנו חניה לסבא וסבתא ממש ליד הכניסה.", CreatedAt = At(2026, 9, 28, 19) },
            new EventUpdate { Id = 3, EventId = 2, AuthorId = 2, Content = "האולם סגור! עכשיו רק לא לגלות לסבתא.", CreatedAt = At(2026, 9, 30, 20) });
    }

    private static User User(int id, string fullName, string email) => new()
    {
        Id = id, FullName = fullName, Email = email, PasswordHash = DemoPasswordHash,
        Role = SystemRole.User, IsActive = true, CreatedAt = Created,
    };

    private static FamilyMember Member(int familyId, int userId, FamilyRole role) => new()
    {
        FamilyId = familyId, UserId = userId, Role = role, JoinedAt = Created,
    };

    private static EventParticipant Participant(int eventId, int userId, RsvpStatus status, int guests = 0) => new()
    {
        EventId = eventId, UserId = userId, RsvpStatus = status, GuestsCount = guests,
        InvitedAt = Created, RespondedAt = status == RsvpStatus.Pending ? null : Created.AddDays(2),
    };

    private static FamilyTask Task(int id, int eventId, string title, TaskCategory category, TaskPriority priority,
        FamilyTaskStatus status, DateTime dueDate, int? assigneeId) => new()
    {
        Id = id, EventId = eventId, Title = title, Category = category, Priority = priority, Status = status,
        DueDate = dueDate, AssigneeId = assigneeId, CreatedAt = Created,
        Version = new Guid($"b2000000-0000-0000-0000-{id:D12}"),
    };
}
