using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using my_project.Core.Interfaces;
using my_project.Core.Interfaces.Repositories;
using my_project.Data.Repositories;

namespace my_project.Data;

public static class DependencyInjection
{
    public static IServiceCollection AddDataLayer(this IServiceCollection services, string connectionString)
    {
        // Scoped (the default): one DbContext per request
        services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

        // Scoped like the DbContext they use
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IFamilyRepository, FamilyRepository>();
        services.AddScoped<IFamilyMemberRepository, FamilyMemberRepository>();
        services.AddScoped<IEventRepository, EventRepository>();
        services.AddScoped<IEventParticipantRepository, EventParticipantRepository>();
        services.AddScoped<IEventUpdateRepository, EventUpdateRepository>();
        services.AddScoped<IFamilyTaskRepository, FamilyTaskRepository>();
        services.AddScoped<INotificationSettingsRepository, NotificationSettingsRepository>();
        services.AddScoped<IEventAttachmentRepository, EventAttachmentRepository>();
        services.AddScoped<ISentNotificationRepository, SentNotificationRepository>();

        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}
