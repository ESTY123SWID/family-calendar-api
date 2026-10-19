using AutoMapper;
using my_project.Core.Common;
using my_project.Core.DTOs.Notifications;
using my_project.Core.Entities;
using my_project.Core.Interfaces;
using my_project.Core.Interfaces.Infrastructure;
using my_project.Core.Interfaces.Services;

namespace my_project.Service.Services;

public class NotificationSettingsService : INotificationSettingsService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IMapper _mapper;

    public NotificationSettingsService(IUnitOfWork unitOfWork, ICurrentUser currentUser, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _mapper = mapper;
    }

    public async Task<ServiceResult<NotificationSettingsDto>> GetMineAsync(CancellationToken cancellationToken = default)
    {
        // Settings are created on registration; if the row is missing, the defaults apply
        var settings = await _unitOfWork.NotificationSettings.GetByUserIdAsync(_currentUser.UserId, cancellationToken)
            ?? new NotificationSettings { UserId = _currentUser.UserId };

        return ServiceResult<NotificationSettingsDto>.Ok(_mapper.Map<NotificationSettingsDto>(settings));
    }

    public async Task<ServiceResult> UpdateMineAsync(
        UpdateNotificationSettingsRequest request,
        CancellationToken cancellationToken = default)
    {
        var settings = await _unitOfWork.NotificationSettings.GetByUserIdAsync(_currentUser.UserId, cancellationToken);

        if (settings is null)
        {
            settings = new NotificationSettings { UserId = _currentUser.UserId };
            await _unitOfWork.NotificationSettings.AddAsync(settings, cancellationToken);
        }

        settings.EventReminders = request.EventReminders;
        settings.DaysBeforeEvent = request.DaysBeforeEvent;
        settings.EventDayReminder = request.EventDayReminder;
        settings.TaskReminders = request.TaskReminders;
        settings.TaskChangeNotices = request.TaskChangeNotices;
        settings.EventChangeNotices = request.EventChangeNotices;
        settings.InvitationNotices = request.InvitationNotices;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ServiceResult.Ok();
    }
}
