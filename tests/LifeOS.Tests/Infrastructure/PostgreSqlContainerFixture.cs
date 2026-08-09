using LifeOS.Core.Abstractions;
using LifeOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace LifeOS.Tests.Infrastructure;

public sealed class PostgreSqlContainerFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container =
        new PostgreSqlBuilder("postgres:17")
            .WithDatabase("lifeos_tests")
            .WithUsername("lifeos")
            .WithPassword("lifeos_test")
            .Build();

    public string ConnectionString =>
        _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        await using var context =
            CreateDbContext();

        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }

    public AppDbContext CreateDbContext(
        DateTimeOffset? utcNow = null)
    {
        var options =
            new DbContextOptionsBuilder<AppDbContext>()
                .UseNpgsql(ConnectionString)
                .Options;

        return new AppDbContext(
            options,
            new TestDateTimeProvider(
                utcNow ??
                new DateTimeOffset(
                    2026,
                    8,
                    9,
                    12,
                    0,
                    0,
                    TimeSpan.Zero)));
    }

    private sealed class TestDateTimeProvider
        : IDateTimeProvider
    {
        public TestDateTimeProvider(
            DateTimeOffset utcNow)
        {
            UtcNow = utcNow;
        }

        public DateTimeOffset UtcNow { get; }

        public DateOnly GetCurrentDate(
            string timeZoneId)
        {
            var timeZone =
                TimeZoneInfo.FindSystemTimeZoneById(
                    timeZoneId);

            var local =
                TimeZoneInfo.ConvertTime(
                    UtcNow,
                    timeZone);

            return DateOnly.FromDateTime(
                local.DateTime);
        }

        public bool IsValidTimeZone(
            string timeZoneId)
        {
            try
            {
                TimeZoneInfo.FindSystemTimeZoneById(
                    timeZoneId);

                return true;
            }
            catch (TimeZoneNotFoundException)
            {
                return false;
            }
            catch (InvalidTimeZoneException)
            {
                return false;
            }
        }
    }
}