using LifeOS.Core.Abstractions;
using LifeOS.Web.Options;
using LifeOS.Web.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LifeOS.Web.Extensions;

public static class DependencyInjection
{
    public static IServiceCollection AddWeb(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<DevelopmentUserOptions>(
            configuration.GetSection(
                DevelopmentUserOptions.SectionName));

        services.AddScoped<ICurrentUserService,
            DevelopmentCurrentUserService>();
        services.AddScoped<NotificationRefreshCoordinator>();

        return services;
    }
}