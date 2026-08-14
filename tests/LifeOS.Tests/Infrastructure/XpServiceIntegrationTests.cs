using LifeOS.Core.Abstractions;
using LifeOS.Core.DTOs;
using LifeOS.Core.DTOs.Xp;
using LifeOS.Core.Entities;
using LifeOS.Core.Enums;
using LifeOS.Core.Enums.Xp;
using LifeOS.Core.Progression;
using LifeOS.Core.Services;
using LifeOS.Infrastructure.Persistence;
using LifeOS.Infrastructure.Repositories;
using LifeOS.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace LifeOS.Tests.Infrastructure;

public sealed class XpServiceIntegrationTests : IClassFixture<PostgreSqlContainerFixture>
{
    private static readonly DateOnly BusinessDate = new(2026, 8, 12);
    private readonly PostgreSqlContainerFixture _fixture;

    public XpServiceIntegrationTests(PostgreSqlContainerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task SameKeyContention_ProducesOneAwardAndOneDuplicate()
    {
        await ResetDatabaseAsync();
        var userId = Guid.NewGuid();
        var sourceId = Guid.NewGuid();
        var services = CreateServices(userId);
        var award = CreateAward(sourceId);

        var results = await Task.WhenAll(
            services.service.AwardQuestXpAsync(award),
            services.service.AwardQuestXpAsync(award));

        Assert.Single(results, result => !result.IsDuplicate && result.AwardedXp == 100);
        Assert.Single(results, result => result.IsDuplicate && result.AwardedXp == 0);
        await AssertStateAsync(userId, 1, 100, 100, 1, 1);
    }

    [Fact]
    public async Task DifferentKeyContention_RetriesAndPreservesBothAwards()
    {
        await ResetDatabaseAsync();
        var userId = Guid.NewGuid();
        var first = CreateServices(userId).service;
        var second = CreateServices(userId).service;

        var results = await Task.WhenAll(
            first.AwardQuestXpAsync(CreateAward(Guid.NewGuid())),
            second.AwardQuestXpAsync(CreateAward(Guid.NewGuid())));

        Assert.All(results, result => Assert.Equal(100, result.AwardedXp));
        await AssertStateAsync(userId, 2, 200, 200, 2, 2);
    }

    [Fact]
    public async Task NearCapContention_NeverExceedsDailyCap()
    {
        await ResetDatabaseAsync();
        var userId = Guid.NewGuid();
        var existingId = Guid.NewGuid();
        await using (var context = _fixture.CreateDbContext())
        {
            context.UserProgressions.Add(new UserProgression
            {
                UserId = userId,
                TotalLifetimeXp = 450,
                CurrentLevel = XpRules.CalculateLevel(450),
                CurrentEchelon = XpRules.CalculateEchelon(XpRules.CalculateLevel(450)),
                DailyQuestXpToday = 450,
                DailyQuestXpDate = BusinessDate,
                Version = 1
            });
            context.XpTransactions.Add(new XpTransaction
            {
                UserId = userId,
                Source = XpSource.QuestCompletion,
                SourceType = XpSourceType.Task,
                SourceEntityId = existingId,
                XpAmount = 450,
                OccurredAtUtc = new DateTimeOffset(
                    BusinessDate.ToDateTime(new TimeOnly(12, 0), DateTimeKind.Utc)),
                BusinessDate = BusinessDate,
                IdempotencyKey = "existing"
            });
            await context.SaveChangesAsync();
        }

        var first = CreateServices(userId).service;
        var second = CreateServices(userId).service;
        var results = await Task.WhenAll(
            first.AwardQuestXpAsync(CreateAward(Guid.NewGuid())),
            second.AwardQuestXpAsync(CreateAward(Guid.NewGuid())));

        Assert.Equal(50, results.Sum(result => result.AwardedXp));
        await AssertStateAsync(userId, 2, 500, 500, 2, 2);
        await using var verify = _fixture.CreateDbContext();
        Assert.Equal(500, await verify.XpTransactions
            .Where(item => item.UserId == userId && item.BusinessDate == BusinessDate &&
                item.Source == XpSource.QuestCompletion)
            .SumAsync(item => item.XpAmount));
        Assert.Equal(1, await verify.XpTransactions.CountAsync(item =>
            item.UserId == userId && item.BusinessDate == BusinessDate && item.XpAmount == 50));
        Assert.Equal(0, await verify.XpTransactions.CountAsync(item =>
            item.UserId == userId && item.BusinessDate == BusinessDate && item.XpAmount == 0));
    }

    private (XpService service, Mock<IUserSettingsService> settings) CreateServices(Guid userId)
    {
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.SetupGet(item => item.IsAuthenticated).Returns(true);
        currentUser.SetupGet(item => item.UserId).Returns(userId);
        var settings = new Mock<IUserSettingsService>();
        settings.Setup(item => item.GetCurrentUserSettingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserSettingsDto { UserId = userId, TimeZoneId = "UTC" });
        var dateTime = new Mock<IDateTimeProvider>();
        dateTime.SetupGet(item => item.UtcNow).Returns(new DateTimeOffset(2026, 8, 12, 12, 0, 0, TimeSpan.Zero));
        dateTime.Setup(item => item.GetCurrentDate("UTC")).Returns(BusinessDate);
        var repository = new XpRepository(new TestDbContextFactory(_fixture));
        return (new XpService(repository, currentUser.Object, settings.Object, dateTime.Object,
            NullLogger<XpService>.Instance), settings);
    }

    private static AwardQuestXpDto CreateAward(Guid sourceId) => new()
    {
        SourceType = XpSourceType.Task,
        SourceEntityId = sourceId,
        OccurredAtUtc = new DateTimeOffset(2026, 8, 12, 12, 0, 0, TimeSpan.Zero),
        BusinessDate = BusinessDate,
        EstimatedTime = EstimatedTime.Between15And30Minutes,
        FrictionLevel = FrictionLevel.Low
    };

    private async Task AssertStateAsync(Guid userId, int transactions, long lifetime,
        int daily, long version, int expectedDailyTransactions)
    {
        await using var context = _fixture.CreateDbContext();
        Assert.Equal(transactions, await context.XpTransactions.CountAsync(item => item.UserId == userId));
        Assert.Equal(expectedDailyTransactions, await context.XpTransactions.CountAsync(item =>
            item.UserId == userId && item.BusinessDate == BusinessDate));
        var progression = await context.UserProgressions.SingleAsync(item => item.UserId == userId);
        Assert.Equal(lifetime, progression.TotalLifetimeXp);
        Assert.Equal(daily, progression.DailyQuestXpToday);
        Assert.Equal(version, progression.Version);
    }

    private async Task ResetDatabaseAsync()
    {
        await using var context = _fixture.CreateDbContext();
        await context.XpTransactions.IgnoreQueryFilters().ExecuteDeleteAsync();
        await context.UserProgressions.IgnoreQueryFilters().ExecuteDeleteAsync();
    }

    private sealed class TestDbContextFactory : IDbContextFactory<AppDbContext>
    {
        private readonly PostgreSqlContainerFixture _fixture;
        public TestDbContextFactory(PostgreSqlContainerFixture fixture) => _fixture = fixture;
        public AppDbContext CreateDbContext() => _fixture.CreateDbContext();
        public Task<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(_fixture.CreateDbContext());
    }
}
