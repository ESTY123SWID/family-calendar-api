using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using my_project.Core.Interfaces.Infrastructure;
using my_project.Core.Interfaces.Services;
using my_project.Service.Common;
using my_project.Service.Infrastructure;
using my_project.Service.Notifications;
using my_project.Service.Options;
using my_project.Service.Security;
using my_project.Service.Services;

namespace my_project.Service;

// Not registered here (they belong to the API / Data layers):
// ICurrentUser, IEmailSender, IFileStorage, and the Options (JwtSettings, UploadSettings, DisplaySettings)
public static class DependencyInjection
{
    public static IServiceCollection AddServiceLayer(this IServiceCollection services)
    {
        services.AddAutoMapper(typeof(DependencyInjection).Assembly);

        // Stateless – one instance for the whole application
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<ITokenService, JwtTokenService>();
        services.AddSingleton<LocalTimeConverter>();
        services.AddSingleton<EmailTemplates>();

        // Validation rules of options that only this layer can check (the binding itself is done by the API)
        services.AddSingleton<IValidateOptions<UploadSettings>, UploadSettingsValidator>();

        // Scoped – they use the request's IUnitOfWork (DbContext) and ICurrentUser
        services.AddScoped<IFamilyAccessService, FamilyAccessService>();
        services.AddScoped<FamilyDeleter>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IFamilyService, FamilyService>();
        services.AddScoped<IEventService, EventService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IParticipantService, ParticipantService>();
        services.AddScoped<IEventUpdateService, EventUpdateService>();
        services.AddScoped<ITaskService, TaskService>();
        services.AddScoped<IAttachmentService, AttachmentService>();
        services.AddScoped<INotificationSettingsService, NotificationSettingsService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IReminderService, ReminderService>();
        services.AddScoped<IAdminService, AdminService>();

        return services;
    }
}
