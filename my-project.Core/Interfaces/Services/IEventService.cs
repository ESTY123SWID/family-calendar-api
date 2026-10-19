using my_project.Core.Common;
using my_project.Core.DTOs.Events;

namespace my_project.Core.Interfaces.Services;

public interface IEventService
{
    Task<ServiceResult<PagedResult<EventSummaryDto>>> GetPagedAsync(
        int familyId,
        EventQueryParameters query,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<EventDetailsDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    // Any family member. The creator becomes the organizer
    Task<ServiceResult<EventDetailsDto>> CreateAsync(
        int familyId,
        CreateEventRequest request,
        CancellationToken cancellationToken = default);

    // Organizer or family manager. A stale Version → Conflict
    Task<ServiceResult> UpdateAsync(int id, UpdateEventRequest request, CancellationToken cancellationToken = default);

    // Organizer or family manager. Sets the status to Cancelled (no physical delete)
    Task<ServiceResult> CancelAsync(int id, CancellationToken cancellationToken = default);
}
