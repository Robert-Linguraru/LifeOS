using LifeOS.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace LifeOS.Tests.Infrastructure;

public sealed class UserSettingsPersistenceIntegrationTests
    : IClassFixture<PostgreSqlContainerFixture>
{
    private readonly PostgreSqlContainerFixture _fixture;

    public UserSettingsPersistenceIntegrationTests(
        PostgreSqlContainerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task FreshSettingsRemainUnconfirmedAndConfirmationTimestampPersists()
    {
        var userId = Guid.NewGuid();
        var confirmedAtUtc = new DateTimeOffset(
            2026,
            8,
            15,
            12,
            0,
            0,
            TimeSpan.Zero);

        await using (var context = _fixture.CreateDbContext())
        {
            context.UserSettings.Add(
                new UserSettings
                {
                    UserId = userId,
                    TimeZoneId = "UTC"
                });

            await context.SaveChangesAsync();
        }

        await using (var context = _fixture.CreateDbContext())
        {
            var settings = await context.UserSettings
                .SingleAsync(item => item.UserId == userId);

            Assert.Equal("UTC", settings.TimeZoneId);
            Assert.Null(settings.TimeZoneConfiguredAtUtc);

            settings.TimeZoneConfiguredAtUtc = confirmedAtUtc;
            settings.TimeZoneId = "Europe/Bucharest";
            await context.SaveChangesAsync();
        }

        await using (var context = _fixture.CreateDbContext())
        {
            var settings = await context.UserSettings
                .SingleAsync(item => item.UserId == userId);

            Assert.Equal("Europe/Bucharest", settings.TimeZoneId);
            Assert.Equal(confirmedAtUtc, settings.TimeZoneConfiguredAtUtc);
            Assert.Equal(
                1,
                await context.UserSettings.CountAsync(
                    item => item.UserId == userId));
        }
    }
}
