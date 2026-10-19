using System.Globalization;
using my_project.Core.Entities;
using my_project.Core.Enums;
using my_project.Core.Models;
using my_project.Service.Common;

namespace my_project.Service.Notifications;

// The text of every email the system sends (Hebrew, plain text). Times are shown in the display time zone
public class EmailTemplates
{
    private readonly LocalTimeConverter _localTime;

    public EmailTemplates(LocalTimeConverter localTime)
    {
        _localTime = localTime;
    }

    public EmailMessage EventChanged(User to, Event evt) => new(
        to.Email,
        $"האירוע \"{evt.Title}\" עודכן",
        $"שלום {to.FullName},\n\nפרטי האירוע \"{evt.Title}\" עודכנו.\nמועד: {FormatTime(evt.StartAt)}\nכתובת: {evt.Address}");

    public EmailMessage EventCancelled(User to, Event evt) => new(
        to.Email,
        $"האירוע \"{evt.Title}\" בוטל",
        $"שלום {to.FullName},\n\nהאירוע \"{evt.Title}\" שתוכנן ל-{FormatTime(evt.StartAt)} בוטל.");

    public EmailMessage EventUpdatePosted(User to, Event evt, EventUpdate update) => new(
        to.Email,
        $"עדכון חדש לאירוע \"{evt.Title}\"",
        $"שלום {to.FullName},\n\nפורסם עדכון לאירוע \"{evt.Title}\":\n\n{update.Content}");

    public EmailMessage Invitation(User to, Event evt) => new(
        to.Email,
        $"הוזמנת לאירוע \"{evt.Title}\"",
        $"שלום {to.FullName},\n\nהוזמנת לאירוע \"{evt.Title}\".\nמועד: {FormatTime(evt.StartAt)}\nכתובת: {evt.Address}\n\nנשמח לאישור הגעה.");

    public EmailMessage TaskChanged(User to, FamilyTask task) => new(
        to.Email,
        $"המשימה \"{task.Title}\" עודכנה",
        $"שלום {to.FullName},\n\nהמשימה \"{task.Title}\" שבאחריותך עודכנה. סטטוס נוכחי: {StatusName(task.Status)}.");

    public EmailMessage EventReminder(User to, Event evt) => new(
        to.Email,
        $"תזכורת: \"{evt.Title}\" מתקרב",
        $"שלום {to.FullName},\n\nתזכורת לאירוע \"{evt.Title}\".\nמועד: {FormatTime(evt.StartAt)}\nכתובת: {evt.Address}");

    public EmailMessage EventDayReminder(User to, Event evt) => new(
        to.Email,
        $"היום: \"{evt.Title}\"",
        $"שלום {to.FullName},\n\nהאירוע \"{evt.Title}\" מתקיים היום.\nמועד: {FormatTime(evt.StartAt)}\nכתובת: {evt.Address}");

    public EmailMessage RsvpReminder(User to, Event evt) => new(
        to.Email,
        $"עדיין לא אישרת הגעה ל\"{evt.Title}\"",
        $"שלום {to.FullName},\n\nטרם אישרת הגעה לאירוע \"{evt.Title}\" ({FormatTime(evt.StartAt)}). נשמח לתשובה.");

    public EmailMessage TaskReminder(User to, FamilyTask task) => new(
        to.Email,
        $"תזכורת: המשימה \"{task.Title}\"",
        $"שלום {to.FullName},\n\nמועד היעד של המשימה \"{task.Title}\" ({task.Event.Title}) מתקרב: {FormatTime(task.DueDate!.Value)}.");

    public EmailMessage TaskOverdue(User to, FamilyTask task) => new(
        to.Email,
        $"המשימה \"{task.Title}\" באיחור",
        $"שלום {to.FullName},\n\nמועד היעד של המשימה \"{task.Title}\" ({task.Event.Title}) עבר: {FormatTime(task.DueDate!.Value)}.");

    private string FormatTime(DateTime utc)
    {
        return _localTime.ToLocal(utc).ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);
    }

    private static string StatusName(FamilyTaskStatus status) => status switch
    {
        FamilyTaskStatus.Open => "פתוחה",
        FamilyTaskStatus.Assigned => "שובצה",
        FamilyTaskStatus.InProgress => "בביצוע",
        _ => "הושלמה"
    };
}
