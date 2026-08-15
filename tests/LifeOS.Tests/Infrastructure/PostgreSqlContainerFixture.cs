using LifeOS.Core.Abstractions;
using LifeOS.Core.Time;
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

    public IDbContextFactory<AppDbContext> CreateDbContextFactory()
    {
        return new TestDbContextFactory(this);
    }

    private sealed class TestDbContextFactory
        : IDbContextFactory<AppDbContext>
    {
        private readonly PostgreSqlContainerFixture _fixture;

        public TestDbContextFactory(PostgreSqlContainerFixture fixture)
        {
            _fixture = fixture;
        }

        public AppDbContext CreateDbContext()
        {
            return _fixture.CreateDbContext();
        }

        public Task<AppDbContext> CreateDbContextAsync(
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_fixture.CreateDbContext());
        }
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

        public LocalTimeConversionResult ConvertLocalToUtc(
            DateOnly localDate,
            TimeOnly localTime,
            string timeZoneId)
        {
            return LocalTimeConversionResult.Success(
                new DateTimeOffset(
                    localDate.ToDateTime(localTime),
                    TimeSpan.Zero));
        }

        public DateTimeOffset ConvertUtcToLocal(
            DateTimeOffset utcInstant,
            string timeZoneId)
        {
            return utcInstant;
        }

        public IReadOnlyList<string> GetTimeZoneIds()
        {
            return ["UTC"];
        }
    }
}