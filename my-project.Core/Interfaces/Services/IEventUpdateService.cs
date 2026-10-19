using my_project.Core.Common;
using my_project.Core.DTOs.Updates;

namespace my_project.Core.Interfaces.Services;

public interface IEventUpdateService
{
    Task<ServiceResult<IReadOnlyList<EventUpdateDto>>> GetForEventAsync(int eventId, CancellationToken cancellationToken = default);

    // Organizer or family manager
    Task<ServiceResult<EventUpdateDto>> CreateAsync(
        int eventId,
        CreateEventUpdateRequest request,
        CancellationToken cancellationToken = default);

    // Organizer or family manager
    Task<ServiceResult> DeleteAsync(int eventId, int updateId, CancellationToken cancellationToken = default);
}
