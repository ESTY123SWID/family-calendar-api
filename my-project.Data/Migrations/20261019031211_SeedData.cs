using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace my_project.Data.Migrations
{
    /// <inheritdoc />
    public partial class SeedData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "CreatedAt", "Email", "FullName", "IsActive", "PasswordHash", "Role" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 9, 1, 8, 0, 0, 0, DateTimeKind.Utc), "sara@family.demo", "שרה לוי", true, "AQAAAAIAAYagAAAAEMkwev6h9CPGAXrtMAsHJNPVkFlvphwSyc+MylHnzACrYX9x/hUVUvk0S4sPDRzKiQ==", 0 },
                    { 2, new DateTime(2026, 9, 1, 8, 0, 0, 0, DateTimeKind.Utc), "david@family.demo", "דוד לוי", true, "AQAAAAIAAYagAAAAEMkwev6h9CPGAXrtMAsHJNPVkFlvphwSyc+MylHnzACrYX9x/hUVUvk0S4sPDRzKiQ==", 0 },
                    { 3, new DateTime(2026, 9, 1, 8, 0, 0, 0, DateTimeKind.Utc), "moshe@family.demo", "משה לוי", true, "AQAAAAIAAYagAAAAEMkwev6h9CPGAXrtMAsHJNPVkFlvphwSyc+MylHnzACrYX9x/hUVUvk0S4sPDRzKiQ==", 0 },
                    { 4, new DateTime(2026, 9, 1, 8, 0, 0, 0, DateTimeKind.Utc), "rivka@family.demo", "רבקה כהן", true, "AQAAAAIAAYagAAAAEMkwev6h9CPGAXrtMAsHJNPVkFlvphwSyc+MylHnzACrYX9x/hUVUvk0S4sPDRzKiQ==", 0 },
                    { 5, new DateTime(2026, 9, 1, 8, 0, 0, 0, DateTimeKind.Utc), "noa@family.demo", "נועה לוי", true, "AQAAAAIAAYagAAAAEMkwev6h9CPGAXrtMAsHJNPVkFlvphwSyc+MylHnzACrYX9x/hUVUvk0S4sPDRzKiQ==", 0 },
                    { 6, new DateTime(2026, 9, 1, 8, 0, 0, 0, DateTimeKind.Utc), "yael@family.demo", "יעל פרידמן", true, "AQAAAAIAAYagAAAAEMkwev6h9CPGAXrtMAsHJNPVkFlvphwSyc+MylHnzACrYX9x/hUVUvk0S4sPDRzKiQ==", 0 }
                });

            migrationBuilder.InsertData(
                table: "Families",
                columns: new[] { "Id", "CreatedAt", "CreatedById", "JoinCode", "Name" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 9, 1, 8, 0, 0, 0, DateTimeKind.Utc), 1, "LEVI-4821", "משפחת לוי" },
                    { 2, new DateTime(2026, 9, 1, 8, 0, 0, 0, DateTimeKind.Utc), 4, "COHEN-3307", "משפחת כהן" }
                });

            migrationBuilder.InsertData(
                table: "NotificationSettings",
                columns: new[] { "UserId", "DaysBeforeEvent", "EventChangeNotices", "EventDayReminder", "EventReminders", "InvitationNotices", "TaskChangeNotices", "TaskReminders" },
                values: new object[,]
                {
                    { 1, 3, true, true, true, true, true, true },
                    { 2, 3, true, true, true, true, true, true },
                    { 3, 3, true, true, true, true, true, true },
                    { 4, 3, true, true, true, true, true, true },
                    { 5, 3, true, true, true, true, true, true },
                    { 6, 3, true, true, true, true, true, true }
                });

            migrationBuilder.InsertData(
                table: "Events",
                columns: new[] { "Id", "Address", "CreatedAt", "Description", "Directions", "EndAt", "FamilyId", "LocationName", "OrganizerId", "StartAt", "Status", "Title", "UpdatedAt", "Version" },
                values: new object[,]
                {
                    { 1, "רחוב הזית 12, ירושלים", new DateTime(2026, 9, 1, 8, 0, 0, 0, DateTimeKind.Utc), "שבת של כל המשפחה יחד. מביאים מצב רוח טוב – את השאר מחלקים במשימות.", "חניה בחניון הציבורי ברחוב הסמוך. כניסה ב׳, קומה 2.", new DateTime(2027, 1, 16, 18, 0, 0, 0, DateTimeKind.Utc), 1, "הבית של סבא וסבתא", 1, new DateTime(2027, 1, 15, 14, 45, 0, 0, DateTimeKind.Utc), 0, "שבת משפחתית אצל סבא וסבתא", null, new Guid("a1000000-0000-0000-0000-000000000001") },
                    { 2, "רחוב ההגנה 5, פתח תקווה", new DateTime(2026, 9, 1, 8, 0, 0, 0, DateTimeKind.Utc), "ערב הפתעה – לא לספר לסבתא!", "חניה חופשית מאחורי האולם.", new DateTime(2027, 2, 3, 21, 0, 0, 0, DateTimeKind.Utc), 1, "אולם הגפן", 2, new DateTime(2027, 2, 3, 17, 30, 0, 0, DateTimeKind.Utc), 0, "יום הולדת 80 לסבתא רחל", null, new Guid("a1000000-0000-0000-0000-000000000002") },
                    { 3, "רחוב הדקל 3, ירושלים", new DateTime(2026, 9, 1, 8, 0, 0, 0, DateTimeKind.Utc), null, null, null, 1, "המתנ״ס השכונתי", 1, new DateTime(2027, 3, 23, 15, 0, 0, 0, DateTimeKind.Utc), 1, "מסיבת פורים לילדים", null, new Guid("a1000000-0000-0000-0000-000000000003") },
                    { 4, "שדרות רוטשילד 40, תל אביב", new DateTime(2026, 9, 1, 8, 0, 0, 0, DateTimeKind.Utc), "ארוחה חגיגית לכבוד המעבר לדירה החדשה.", "קומה 4, יש מעלית.", null, 2, "הדירה החדשה", 4, new DateTime(2027, 1, 22, 17, 0, 0, 0, DateTimeKind.Utc), 0, "ארוחת שישי אצל רבקה", null, new Guid("a1000000-0000-0000-0000-000000000004") }
                });

            migrationBuilder.InsertData(
                table: "FamilyMembers",
                columns: new[] { "FamilyId", "UserId", "JoinedAt", "Role" },
                values: new object[,]
                {
                    { 1, 1, new DateTime(2026, 9, 1, 8, 0, 0, 0, DateTimeKind.Utc), 1 },
                    { 1, 2, new DateTime(2026, 9, 1, 8, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 1, 3, new DateTime(2026, 9, 1, 8, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 1, 5, new DateTime(2026, 9, 1, 8, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 1, 6, new DateTime(2026, 9, 1, 8, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 2, 2, new DateTime(2026, 9, 1, 8, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 2, 4, new DateTime(2026, 9, 1, 8, 0, 0, 0, DateTimeKind.Utc), 1 }
                });

            migrationBuilder.InsertData(
                table: "EventParticipants",
                columns: new[] { "EventId", "UserId", "GuestsCount", "InvitedAt", "RespondedAt", "RsvpStatus" },
                values: new object[,]
                {
                    { 1, 1, 3, new DateTime(2026, 9, 1, 8, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 9, 3, 8, 0, 0, 0, DateTimeKind.Utc), 1 },
                    { 1, 2, 4, new DateTime(2026, 9, 1, 8, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 9, 3, 8, 0, 0, 0, DateTimeKind.Utc), 1 },
                    { 1, 3, 1, new DateTime(2026, 9, 1, 8, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 9, 3, 8, 0, 0, 0, DateTimeKind.Utc), 1 },
                    { 1, 5, 2, new DateTime(2026, 9, 1, 8, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 9, 3, 8, 0, 0, 0, DateTimeKind.Utc), 3 },
                    { 1, 6, 0, new DateTime(2026, 9, 1, 8, 0, 0, 0, DateTimeKind.Utc), null, 0 },
                    { 2, 1, 1, new DateTime(2026, 9, 1, 8, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 9, 3, 8, 0, 0, 0, DateTimeKind.Utc), 1 },
                    { 2, 2, 2, new DateTime(2026, 9, 1, 8, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 9, 3, 8, 0, 0, 0, DateTimeKind.Utc), 1 },
                    { 2, 3, 0, new DateTime(2026, 9, 1, 8, 0, 0, 0, DateTimeKind.Utc), null, 0 },
                    { 2, 5, 0, new DateTime(2026, 9, 1, 8, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 9, 3, 8, 0, 0, 0, DateTimeKind.Utc), 2 },
                    { 2, 6, 0, new DateTime(2026, 9, 1, 8, 0, 0, 0, DateTimeKind.Utc), null, 0 },
                    { 3, 1, 0, new DateTime(2026, 9, 1, 8, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 9, 3, 8, 0, 0, 0, DateTimeKind.Utc), 1 },
                    { 3, 2, 0, new DateTime(2026, 9, 1, 8, 0, 0, 0, DateTimeKind.Utc), null, 0 },
                    { 4, 2, 0, new DateTime(2026, 9, 1, 8, 0, 0, 0, DateTimeKind.Utc), null, 0 },
                    { 4, 4, 1, new DateTime(2026, 9, 1, 8, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 9, 3, 8, 0, 0, 0, DateTimeKind.Utc), 1 }
                });

            migrationBuilder.InsertData(
                table: "EventUpdates",
                columns: new[] { "Id", "AuthorId", "Content", "CreatedAt", "EventId" },
                values: new object[,]
                {
                    { 1, 1, "שעת הכניסה הוקדמה ל־16:45. מי שמביא אוכל – עד 15:30, בבקשה.", new DateTime(2026, 10, 1, 9, 0, 0, 0, DateTimeKind.Utc), 1 },
                    { 2, 1, "שמרנו חניה לסבא וסבתא ממש ליד הכניסה.", new DateTime(2026, 9, 28, 19, 0, 0, 0, DateTimeKind.Utc), 1 },
                    { 3, 2, "האולם סגור! עכשיו רק לא לגלות לסבתא.", new DateTime(2026, 9, 30, 20, 0, 0, 0, DateTimeKind.Utc), 2 }
                });

            migrationBuilder.InsertData(
                table: "FamilyTasks",
                columns: new[] { "Id", "AssigneeId", "Category", "CreatedAt", "Description", "DueDate", "EventId", "Priority", "Status", "Title", "UpdatedAt", "Version" },
                values: new object[,]
                {
                    { 1, 2, 0, new DateTime(2026, 9, 1, 8, 0, 0, 0, DateTimeKind.Utc), null, new DateTime(2027, 1, 13, 10, 0, 0, 0, DateTimeKind.Utc), 1, 3, 2, "להזמין עוגות לקינוח", null, new Guid("b2000000-0000-0000-0000-000000000001") },
                    { 2, null, 3, new DateTime(2026, 9, 1, 8, 0, 0, 0, DateTimeKind.Utc), null, new DateTime(2027, 1, 14, 16, 0, 0, 0, DateTimeKind.Utc), 1, 2, 0, "להביא מיחם ופלטה", null, new Guid("b2000000-0000-0000-0000-000000000002") },
                    { 3, null, 1, new DateTime(2026, 9, 1, 8, 0, 0, 0, DateTimeKind.Utc), null, new DateTime(2027, 1, 14, 18, 0, 0, 0, DateTimeKind.Utc), 1, 1, 0, "קניית כלים חד־פעמיים ומפות", null, new Guid("b2000000-0000-0000-0000-000000000003") },
                    { 4, null, 2, new DateTime(2026, 9, 1, 8, 0, 0, 0, DateTimeKind.Utc), null, new DateTime(2027, 1, 15, 12, 0, 0, 0, DateTimeKind.Utc), 1, 1, 0, "לסדר שולחנות וכיסאות", null, new Guid("b2000000-0000-0000-0000-000000000004") },
                    { 5, 5, 0, new DateTime(2026, 9, 1, 8, 0, 0, 0, DateTimeKind.Utc), null, new DateTime(2027, 1, 15, 10, 0, 0, 0, DateTimeKind.Utc), 1, 2, 1, "להכין סלטים", null, new Guid("b2000000-0000-0000-0000-000000000005") },
                    { 6, 3, 3, new DateTime(2026, 9, 1, 8, 0, 0, 0, DateTimeKind.Utc), null, new DateTime(2027, 1, 14, 10, 0, 0, 0, DateTimeKind.Utc), 1, 1, 3, "להביא כיסאות מתקפלים", null, new Guid("b2000000-0000-0000-0000-000000000006") },
                    { 7, 2, 0, new DateTime(2026, 9, 1, 8, 0, 0, 0, DateTimeKind.Utc), null, new DateTime(2027, 1, 31, 10, 0, 0, 0, DateTimeKind.Utc), 2, 2, 1, "להזמין את העוגה", null, new Guid("b2000000-0000-0000-0000-000000000007") },
                    { 8, null, 4, new DateTime(2026, 9, 1, 8, 0, 0, 0, DateTimeKind.Utc), null, new DateTime(2027, 2, 1, 18, 0, 0, 0, DateTimeKind.Utc), 2, 1, 0, "מצגת תמונות של סבתא", null, new Guid("b2000000-0000-0000-0000-000000000008") },
                    { 9, null, 1, new DateTime(2026, 9, 1, 8, 0, 0, 0, DateTimeKind.Utc), null, new DateTime(2027, 1, 21, 16, 0, 0, 0, DateTimeKind.Utc), 4, 1, 0, "להביא יין", null, new Guid("b2000000-0000-0000-0000-000000000009") }
                });

            // The rows above were inserted with explicit Ids, which does not advance the identity sequences.
            // Restart each one after the last seeded Id, so the next row created by the app does not get Id 1 again.
            // (ALTER ... RESTART also works inside the DO block of an idempotent script, unlike a bare SELECT setval)
            var nextIds = new[] { ("Users", 7), ("Families", 3), ("Events", 5), ("FamilyTasks", 10), ("EventUpdates", 4) };
            foreach (var (table, nextId) in nextIds)
            {
                migrationBuilder.Sql($"ALTER TABLE \"{table}\" ALTER COLUMN \"Id\" RESTART WITH {nextId};");
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "EventParticipants",
                keyColumns: new[] { "EventId", "UserId" },
                keyValues: new object[] { 1, 1 });

            migrationBuilder.DeleteData(
                table: "EventParticipants",
                keyColumns: new[] { "EventId", "UserId" },
                keyValues: new object[] { 1, 2 });

            migrationBuilder.DeleteData(
                table: "EventParticipants",
                keyColumns: new[] { "EventId", "UserId" },
                keyValues: new object[] { 1, 3 });

            migrationBuilder.DeleteData(
                table: "EventParticipants",
                keyColumns: new[] { "EventId", "UserId" },
                keyValues: new object[] { 1, 5 });

            migrationBuilder.DeleteData(
                table: "EventParticipants",
                keyColumns: new[] { "EventId", "UserId" },
                keyValues: new object[] { 1, 6 });

            migrationBuilder.DeleteData(
                table: "EventParticipants",
                keyColumns: new[] { "EventId", "UserId" },
                keyValues: new object[] { 2, 1 });

            migrationBuilder.DeleteData(
                table: "EventParticipants",
                keyColumns: new[] { "EventId", "UserId" },
                keyValues: new object[] { 2, 2 });

            migrationBuilder.DeleteData(
                table: "EventParticipants",
                keyColumns: new[] { "EventId", "UserId" },
                keyValues: new object[] { 2, 3 });

            migrationBuilder.DeleteData(
                table: "EventParticipants",
                keyColumns: new[] { "EventId", "UserId" },
                keyValues: new object[] { 2, 5 });

            migrationBuilder.DeleteData(
                table: "EventParticipants",
                keyColumns: new[] { "EventId", "UserId" },
                keyValues: new object[] { 2, 6 });

            migrationBuilder.DeleteData(
                table: "EventParticipants",
                keyColumns: new[] { "EventId", "UserId" },
                keyValues: new object[] { 3, 1 });

            migrationBuilder.DeleteData(
                table: "EventParticipants",
                keyColumns: new[] { "EventId", "UserId" },
                keyValues: new object[] { 3, 2 });

            migrationBuilder.DeleteData(
                table: "EventParticipants",
                keyColumns: new[] { "EventId", "UserId" },
                keyValues: new object[] { 4, 2 });

            migrationBuilder.DeleteData(
                table: "EventParticipants",
                keyColumns: new[] { "EventId", "UserId" },
                keyValues: new object[] { 4, 4 });

            migrationBuilder.DeleteData(
                table: "EventUpdates",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "EventUpdates",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "EventUpdates",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "FamilyMembers",
                keyColumns: new[] { "FamilyId", "UserId" },
                keyValues: new object[] { 1, 1 });

            migrationBuilder.DeleteData(
                table: "FamilyMembers",
                keyColumns: new[] { "FamilyId", "UserId" },
                keyValues: new object[] { 1, 2 });

            migrationBuilder.DeleteData(
                table: "FamilyMembers",
                keyColumns: new[] { "FamilyId", "UserId" },
                keyValues: new object[] { 1, 3 });

            migrationBuilder.DeleteData(
                table: "FamilyMembers",
                keyColumns: new[] { "FamilyId", "UserId" },
                keyValues: new object[] { 1, 5 });

            migrationBuilder.DeleteData(
                table: "FamilyMembers",
                keyColumns: new[] { "FamilyId", "UserId" },
                keyValues: new object[] { 1, 6 });

            migrationBuilder.DeleteData(
                table: "FamilyMembers",
                keyColumns: new[] { "FamilyId", "UserId" },
                keyValues: new object[] { 2, 2 });

            migrationBuilder.DeleteData(
                table: "FamilyMembers",
                keyColumns: new[] { "FamilyId", "UserId" },
                keyValues: new object[] { 2, 4 });

            migrationBuilder.DeleteData(
                table: "FamilyTasks",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "FamilyTasks",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "FamilyTasks",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "FamilyTasks",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "FamilyTasks",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "FamilyTasks",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "FamilyTasks",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "FamilyTasks",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "FamilyTasks",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "NotificationSettings",
                keyColumn: "UserId",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "NotificationSettings",
                keyColumn: "UserId",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "NotificationSettings",
                keyColumn: "UserId",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "NotificationSettings",
                keyColumn: "UserId",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "NotificationSettings",
                keyColumn: "UserId",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "NotificationSettings",
                keyColumn: "UserId",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "Events",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Events",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Events",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Events",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "Families",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Families",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 4);
        }
    }
}
