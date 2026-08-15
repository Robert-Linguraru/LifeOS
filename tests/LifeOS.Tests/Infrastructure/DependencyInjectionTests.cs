using LifeOS.Core.Abstractions;
using LifeOS.Core.Abstractions.Habits;
using LifeOS.Core.Abstractions.Tasks;
using LifeOS.Core.Services;
using LifeOS.Infrastructure.Extensions;
using LifeOS.Infrastructure.Jobs;
using LifeOS.Infrastructure.Options;
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
    public void AddInfrastructure_ShouldRegisterXpRepositoryAndService()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] =
                    "Host=localhost;Port=5433;Database=lifeos;Username=lifeos;Password=test"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddInfrastructure(configuration);

        var repository = services.Single(service => service.ServiceType == typeof(IXpRepository));
        var service = services.Single(item => item.ServiceType == typeof(IXpService));

        Assert.Equal(typeof(XpRepository), repository.ImplementationType);
        Assert.Equal(ServiceLifetime.Scoped, repository.Lifetime);
        Assert.Equal(typeof(XpService), service.ImplementationType);
        Assert.Equal(ServiceLifetime.Scoped, service.Lifetime);
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

    [Fact]
    public void AddInfrastructure_ShouldRegisterReminderJobComposition()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] =
                    "Host=localhost;Port=5433;Database=lifeos;Username=lifeos;Password=test",
                ["ReminderProcessing:BatchSize"] = "100",
                ["ReminderProcessing:AutomaticRetryAttempts"] = "3"
            })
            .Build();
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddInfrastructure(configuration);

        var processing = services.Single(service =>
            service.ServiceType == typeof(IReminderProcessingService));
        var job = services.Single(service =>
            service.ServiceType == typeof(DueReminderJob));

        Assert.Equal(typeof(ReminderProcessingService), processing.ImplementationType);
        Assert.Equal(ServiceLifetime.Scoped, processing.Lifetime);
        Assert.Equal(ServiceLifetime.Scoped, job.Lifetime);

        using var provider = services.BuildServiceProvider();
        Assert.NotNull(provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<ReminderProcessingOptions>>());
        using var scope = provider.CreateScope();
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<DueReminderJob>());
    }




}