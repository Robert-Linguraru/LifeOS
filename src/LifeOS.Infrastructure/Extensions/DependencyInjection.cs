using LifeOS.Core.Abstractions;
using LifeOS.Core.Abstractions.Habits;
using LifeOS.Core.Abstractions.Notifications;
using LifeOS.Core.Abstractions.Reminders;
using LifeOS.Core.Abstractions.Tasks;
using LifeOS.Core.Services;
using LifeOS.Infrastructure.Persistence;
using LifeOS.Infrastructure.Repositories;
using LifeOS.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;


namespace LifeOS.Infrastructure.Extensions;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString =
            configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Connection string 'DefaultConnection' was not found.");

        services.AddDbContextFactory<AppDbContext>(options =>
        {
            options.UseNpgsql(connectionString);
        });

        services.AddScoped<IDashboardService, DashboardService>();

        services.AddScoped<IUserSettingsRepository, UserSettingsRepository>();
        services.AddScoped<IUserSettingsService, UserSettingsService>();
        
        services.AddScoped<ITaskRepository, TaskRepository>();
        services.AddScoped<ITaskService, TaskService>();

        services.AddScoped<IHabitRepository, HabitRepository>();
        services.AddScoped<IHabitService, HabitService>();

        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<INotificationService, NotificationService>();

        services.AddScoped<IReminderRepository, ReminderRepository>();
        services.AddScoped<IReminderService, ReminderService>();

        services.AddScoped<IXpRepository, XpRepository>();
        services.AddScoped<IXpService, XpService>();

        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

        return services;
    }
}