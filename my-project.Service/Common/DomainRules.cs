using my_project.Core.DTOs.Participants;
using my_project.Core.Entities;
using my_project.Core.Enums;

namespace my_project.Service.Common;

// Small calculations shared by several services
internal static class DomainRules
{
    public static bool IsOverdue(FamilyTask task, DateTime nowUtc)
    {
        return task.Status != FamilyTaskStatus.Done
            && task.DueDate.HasValue
            && task.DueDate.Value < nowUtc;
    }

    public static RsvpSummaryDto BuildRsvpSummary(IEnumerable<EventParticipant> participants)
    {
        var summary = new RsvpSummaryDto();

        foreach (var participant in participants)
        {
            switch (participant.RsvpStatus)
            {
                case RsvpStatus.Coming:
                    summary.Coming++;
                    summary.TotalGuests += participant.GuestsCount;
                    break;
                case RsvpStatus.NotComing:
                    summary.NotComing++;
                    break;
                case RsvpStatus.Maybe:
                    summary.Maybe++;
                    break;
                default:
                    summary.Pending++;
                    break;
            }
        }

        return summary;
    }

    // The download link of an attachment (served by the API)
    public static string AttachmentContentUrl(int attachmentId)
    {
        return $"/api/attachments/{attachmentId}/content";
    }
}
