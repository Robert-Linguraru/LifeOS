using LifeOS.Core.Abstractions;
using LifeOS.Core.Abstractions.Tasks;
using LifeOS.Infrastructure.Extensions;
using LifeOS.Infrastructure.Repositories;
using LifeOS.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LifeOS.Tests.Infrastructure;

public sealed class DependencyInjectionTests
{
    [Fact]
    public void AddInfrastructure_ShouldRegisterDateTimeProvider()
    {
        // Arrange
        var configurationValues =
            new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] =
                    "Host=localhost;Port=5433;Database=lifeos;" +
                    "Username=lifeos;Password=test"
            };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configurationValues)
            .Build();

        var services = new ServiceCollection();

        // Act
        services.AddInfrastructure(configuration);

        using var serviceProvider =
            services.BuildServiceProvider();

        var dateTimeProvider =
            serviceProvider.GetService<IDateTimeProvider>();

        // Assert
        Assert.NotNull(dateTimeProvider);
        Assert.IsType<SystemDateTimeProvider>(
            dateTimeProvider);
    }


    [Fact]
    public void AddInfrastructure_ShouldRegisterTaskRepository()
    {
        // Arrange
        var configurationValues =
            new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] =
                    "Host=localhost;Port=5433;Database=lifeos;" +
                    "Username=lifeos;Password=test"
            };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configurationValues)
            .Build();

        var services = new ServiceCollection();

        // Act
        services.AddInfrastructure(configuration);

        using var serviceProvider =
            services.BuildServiceProvider();

        var taskRepository =
            serviceProvider.GetService<ITaskRepository>();

        // Assert
        Assert.NotNull(taskRepository);
        Assert.IsType<TaskRepository>(
            taskRepository);
    }
}