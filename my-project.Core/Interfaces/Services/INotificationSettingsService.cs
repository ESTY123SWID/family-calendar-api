using my_project.Core.Common;
using my_project.Core.DTOs.Notifications;

namespace my_project.Core.Interfaces.Services;

// Every user views and edits only their own settings
public interface INotificationSettingsService
{
    Task<ServiceResult<NotificationSettingsDto>> GetMineAsync(CancellationToken cancellationToken = default);

    Task<ServiceResult> UpdateMineAsync(
        UpdateNotificationSettingsRequest request,
        CancellationToken cancellationToken = default);
}
