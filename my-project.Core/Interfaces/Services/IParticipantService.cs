using my_project.Core.Common;
using my_project.Core.DTOs.Participants;
using my_project.Core.Enums;

namespace my_project.Core.Interfaces.Services;

public interface IParticipantService
{
    // The summary always counts all participants, even when filtered by rsvpStatus
    Task<ServiceResult<EventParticipantsDto>> GetForEventAsync(
        int eventId,
        RsvpStatus? rsvpStatus = null,
        CancellationToken cancellationToken = default);

    // Organizer or family manager. Returns only the newly invited participants
    Task<ServiceResult<IReadOnlyList<ParticipantDto>>> InviteAsync(
        int eventId,
        InviteParticipantsRequest request,
        CancellationToken cancellationToken = default);

    // Organizer or family manager
    Task<ServiceResult> RemoveAsync(int eventId, int userId, CancellationToken cancellationToken = default);

    // The current user answers for themselves only
    Task<ServiceResult> RsvpAsync(int eventId, RsvpRequest request, CancellationToken cancellationToken = default);
}
