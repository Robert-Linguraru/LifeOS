using LifeOS.Core.Abstractions;
using LifeOS.Core.Abstractions.Habits;
using LifeOS.Core.Abstractions.Tasks;
using LifeOS.Core.Services;
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

    [Fact]
    public void AddInfrastructure_ShouldRegisterHabitRepository()
    {
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

        services.AddInfrastructure(configuration);

        var descriptor = services.SingleOrDefault(
            service =>
                service.ServiceType == typeof(IHabitRepository));

        Assert.NotNull(descriptor);
        Assert.Equal(
            typeof(HabitRepository),
            descriptor.ImplementationType);
        Assert.Equal(
            ServiceLifetime.Scoped,
            descriptor.Lifetime);
    }

    [Fact]
    public void AddInfrastructure_ShouldRegisterHabitService()
    {
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

        services.AddInfrastructure(configuration);

        var descriptor = services.SingleOrDefault(
            service =>
                service.ServiceType == typeof(IHabitService));

        Assert.NotNull(descriptor);
        Assert.Equal(
            typeof(HabitService),
            descriptor.ImplementationType);
        Assert.Equal(
            ServiceLifetime.Scoped,
            descriptor.Lifetime);
    }

    [Fact]
    public void AddInfrastructure_ShouldRegisterTaskService()
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

        var descriptor = services.SingleOrDefault(
            service =>
                service.ServiceType == typeof(ITaskService));

        // Assert
        Assert.NotNull(descriptor);
        Assert.Equal(
            typeof(TaskService),
            descriptor.ImplementationType);
        Assert.Equal(
            ServiceLifetime.Scoped,
            descriptor.Lifetime);
    }

    [Fact]
    public void AddInfrastructure_ShouldRegisterDashboardService()
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

        var descriptor = services.SingleOrDefault(
            service =>
                service.ServiceType == typeof(IDashboardService));

        // Assert
        Assert.NotNull(descriptor);
        Assert.Equal(
            typeof(DashboardService),
            descriptor.ImplementationType);
        Assert.Equal(
            ServiceLifetime.Scoped,
            descriptor.Lifetime);
    }




}