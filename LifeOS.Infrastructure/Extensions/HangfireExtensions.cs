using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.Extensions.DependencyInjection;

namespace LifeOS.Infrastructure.Extensions;

public static class HangfireExtensions
{
    public static IServiceCollection AddHangfireWithPostgres(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddHangfire(config =>
            config
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UsePostgreSqlStorage(c =>
                    c.UseNpgsqlConnection(connectionString)));

        services.AddHangfireServer(options =>
        {
            options.WorkerCount = 2; // low footprint for personal app
            options.Queues = new[] { "critical", "default" };
        });

        return services;
    }
}