using LifeOS.Core.Abstractions;
using LifeOS.Core.DTOs.Xp;
using LifeOS.Core.DTOs;
using LifeOS.Core.Entities;
using LifeOS.Core.Enums;
using LifeOS.Core.Enums.Xp;
using LifeOS.Core.Exceptions;
using LifeOS.Core.Services;
using LifeOS.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace LifeOS.Tests.Services;

public sealed class XpServiceTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private readonly Mock<IXpRepository> _repository = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly Mock<IUserSettingsService> _settings = new();
    private readonly Mock<IDateTimeProvider> _dateTime = new();
    private readonly Mock<ILogger<XpService>> _logger = new();

    private XpService CreateService(bool authenticated = true, Guid? userId = null)
    {
        _currentUser.SetupGet(item => item.IsAuthenticated).Returns(authenticated);
        _currentUser.SetupGet(item => item.UserId).Returns(userId ?? UserId);
        return new XpService(_repository.Object, _currentUser.Object, _settings.Object,
            _dateTime.Object, _logger.Object);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, true)]
    public async Task AwardQuestXpAsync_InvalidCurrentUser_DoesNotAccessRepository(
        bool authenticated, bool emptyUserId)
    {
        var service = CreateService(authenticated, emptyUserId ? Guid.Empty : UserId);

        await Assert.ThrowsAsync<CurrentUserUnavailableException>(() =>
            service.AwardQuestXpAsync(CreateAward()));

        _repository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task AwardQuestXpAsync_CommitsFullAward()
    {
        var progression = NewProgression();
        var transaction = new XpTransaction { Id = Guid.NewGuid(), XpAmount = 100 };
        _repository.Setup(item => item.FindByIdempotencyKeyAsync(UserId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((XpTransaction?)null);
        _repository.Setup(item => item.GetOrCreateProgressionAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(progression);
        _repository.Setup(item => item.GetQuestXpSumAsync(UserId, It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        _repository.Setup(item => item.CommitAwardAsync(It.IsAny<XpAwardCommitRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new XpAwardCommitResult
            {
                Status = XpAwardCommitStatus.Committed,
                Transaction = transaction,
                Progression = new UserProgression
                {
                    UserId = UserId, TotalLifetimeXp = 100, CurrentLevel = 1,
                    CurrentEchelon = Echelon.Iron, DailyQuestXpToday = 100,
                    DailyQuestXpDate = new DateOnly(2026, 1, 2), Version = 1
                }
            });

        var result = await CreateService().AwardQuestXpAsync(CreateAward());

        Assert.Equal(100, result.RawXp);
        Assert.Equal(100, result.AwardedXp);
        Assert.False(result.IsDuplicate);
        Assert.Equal(transaction.Id, result.TransactionId);
        _repository.Verify(item => item.CommitAwardAsync(
            It.Is<XpAwardCommitRequest>(request =>
                request.IdempotencyKey == $"TaskComplete:{CreateAward().SourceEntityId:D}" &&
                request.XpAmount == 100 && request.ExpectedVersion == 0),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AwardQuestXpAsync_ExhaustedCap_DoesNotCommit()
    {
        _repository.Setup(item => item.FindByIdempotencyKeyAsync(UserId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((XpTransaction?)null);
        _repository.Setup(item => item.GetOrCreateProgressionAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(NewProgression());
        _repository.Setup(item => item.GetQuestXpSumAsync(UserId, It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(500);

        var result = await CreateService().AwardQuestXpAsync(CreateAward());

        Assert.Equal(0, result.AwardedXp);
        Assert.True(result.IsCapConstrained);
        Assert.Null(result.TransactionId);
        _repository.Verify(item => item.CommitAwardAsync(It.IsAny<XpAwardCommitRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AwardQuestXpAsync_Conflict_RereadsProgressionAndDailySum()
    {
        var firstProgression = NewProgression();
        var secondProgression = NewProgression();
        secondProgression.TotalLifetimeXp = 100;
        secondProgression.Version = 1;
        _repository.Setup(item => item.FindByIdempotencyKeyAsync(UserId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((XpTransaction?)null);
        _repository.SetupSequence(item => item.GetOrCreateProgressionAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(firstProgression).ReturnsAsync(secondProgression);
        _repository.SetupSequence(item => item.GetQuestXpSumAsync(UserId, It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0).ReturnsAsync(100);
        _repository.SetupSequence(item => item.CommitAwardAsync(It.IsAny<XpAwardCommitRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new XpAwardCommitResult { Status = XpAwardCommitStatus.ConcurrencyConflict })
            .ReturnsAsync(new XpAwardCommitResult
            {
                Status = XpAwardCommitStatus.Committed,
                Transaction = new XpTransaction { Id = Guid.NewGuid(), XpAmount = 100 },
                Progression = new UserProgression
                {
                    UserId = UserId, TotalLifetimeXp = 200, CurrentLevel = 7,
                    CurrentEchelon = Echelon.Iron, DailyQuestXpToday = 200,
                    DailyQuestXpDate = new DateOnly(2026, 1, 2), Version = 2
                }
            });

        var result = await CreateService().AwardQuestXpAsync(CreateAward());

        Assert.Equal(100, result.AwardedXp);
        _repository.Verify(item => item.GetOrCreateProgressionAsync(UserId, It.IsAny<CancellationToken>()), Times.Exactly(2));
        _repository.Verify(item => item.GetQuestXpSumAsync(UserId, It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        _repository.Verify(item => item.CommitAwardAsync(
            It.Is<XpAwardCommitRequest>(request => request.ExpectedVersion == 1 && request.ResultingTotalLifetimeXp == 200),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AwardQuestXpAsync_DuplicateCommitResult_ReturnsDuplicate()
    {
        var progression = NewProgression();
        var transaction = new XpTransaction { Id = Guid.NewGuid(), XpAmount = 100 };
        _repository.Setup(item => item.FindByIdempotencyKeyAsync(UserId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((XpTransaction?)null);
        _repository.Setup(item => item.GetOrCreateProgressionAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(progression);
        _repository.Setup(item => item.GetQuestXpSumAsync(UserId, It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        _repository.Setup(item => item.CommitAwardAsync(It.IsAny<XpAwardCommitRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new XpAwardCommitResult
            {
                Status = XpAwardCommitStatus.Duplicate,
                Transaction = transaction,
                Progression = progression
            });

        var result = await CreateService().AwardQuestXpAsync(CreateAward());

        Assert.True(result.IsDuplicate);
        Assert.Equal(0, result.AwardedXp);
        Assert.Equal(transaction.Id, result.TransactionId);
    }

    [Theory]
    [InlineData((XpSourceType)99)]
    [InlineData(XpSourceType.DailyScore)]
    [InlineData(XpSourceType.Streak)]
    public async Task AwardQuestXpAsync_UnsupportedSource_DoesNotAccessRepository(XpSourceType sourceType)
    {
        var award = CreateAward();
        award.SourceType = sourceType;

        await Assert.ThrowsAsync<ValidationException>(() => CreateService().AwardQuestXpAsync(award));

        _repository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task AwardQuestXpAsync_ValidationErrors_DoesNotAccessRepository()
    {
        var cases = new[]
        {
            InvalidAward(sourceEntityId: Guid.Empty),
            InvalidAward(estimatedTime: (EstimatedTime)99),
            InvalidAward(frictionLevel: (FrictionLevel)99)
        };

        foreach (var award in cases)
        {
            await Assert.ThrowsAsync<ValidationException>(() => CreateService().AwardQuestXpAsync(award));
        }

        _repository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task AwardQuestXpAsync_DuplicatePrecheck_ReturnsNormalDuplicate()
    {
        var transaction = new XpTransaction { Id = Guid.NewGuid(), XpAmount = 100 };
        _repository.Setup(item => item.FindByIdempotencyKeyAsync(UserId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction);
        _repository.Setup(item => item.GetOrCreateProgressionAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(NewProgression());

        var result = await CreateService().AwardQuestXpAsync(CreateAward());

        Assert.True(result.IsDuplicate);
        Assert.Equal(0, result.AwardedXp);
        Assert.Equal(transaction.Id, result.TransactionId);
        _repository.Verify(item => item.CommitAwardAsync(It.IsAny<XpAwardCommitRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AwardQuestXpAsync_ThreeConflicts_ThrowsXpConcurrencyException()
    {
        _repository.Setup(item => item.FindByIdempotencyKeyAsync(UserId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((XpTransaction?)null);
        _repository.Setup(item => item.GetOrCreateProgressionAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(NewProgression());
        _repository.Setup(item => item.GetQuestXpSumAsync(UserId, It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        _repository.Setup(item => item.CommitAwardAsync(It.IsAny<XpAwardCommitRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new XpAwardCommitResult { Status = XpAwardCommitStatus.ConcurrencyConflict });

        await Assert.ThrowsAsync<XpConcurrencyException>(() => CreateService().AwardQuestXpAsync(CreateAward()));

        _repository.Verify(item => item.FindByIdempotencyKeyAsync(UserId, It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
        _repository.Verify(item => item.CommitAwardAsync(It.IsAny<XpAwardCommitRequest>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    [Fact]
    public async Task AwardQuestXpAsync_UnrelatedRepositoryError_PropagatesUnchanged()
    {
        var error = new InvalidOperationException("database failure");
        _repository.Setup(item => item.FindByIdempotencyKeyAsync(UserId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(error);

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateService().AwardQuestXpAsync(CreateAward()));

        Assert.Same(error, actual);
    }

    [Fact]
    public async Task GetProgressionAsync_UsesCurrentLocalDateLedgerInsteadOfStaleCache()
    {
        var today = new DateOnly(2026, 1, 3);
        var progression = NewProgression();
        progression.DailyQuestXpToday = 400;
        progression.DailyQuestXpDate = today.AddDays(-1);
        _repository.Setup(item => item.GetOrCreateProgressionAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(progression);
        _settings.Setup(item => item.GetCurrentUserSettingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserSettingsDto { UserId = UserId, TimeZoneId = "Europe/Bucharest" });
        _dateTime.Setup(item => item.GetCurrentDate("Europe/Bucharest")).Returns(today);
        _repository.Setup(item => item.GetQuestXpSumAsync(UserId, today, It.IsAny<CancellationToken>()))
            .ReturnsAsync(75);

        var result = await CreateService().GetProgressionAsync();

        Assert.Equal(today, result.DailyQuestXpDate);
        Assert.Equal(75, result.DailyQuestXpToday);
    }

    [Fact]
    public async Task GetXpHistoryAsync_MapsRepositoryOrderAndDoesNotReadOtherState()
    {
        var first = new XpTransaction { Id = Guid.NewGuid(), UserId = UserId, XpAmount = 100 };
        var second = new XpTransaction { Id = Guid.NewGuid(), UserId = UserId, XpAmount = 50 };
        _repository.Setup(item => item.GetHistoryAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { first, second });

        var result = await CreateService().GetXpHistoryAsync();

        Assert.Equal(new[] { first.Id, second.Id }, result.Select(item => item.Id));
        Assert.Equal(new[] { 100, 50 }, result.Select(item => item.XpAmount));
        _repository.Verify(item => item.GetHistoryAsync(UserId, It.IsAny<CancellationToken>()), Times.Once);
    }

    private static AwardQuestXpDto CreateAward() => new()
    {
        SourceType = XpSourceType.Task,
        SourceEntityId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
        OccurredAtUtc = new DateTimeOffset(2026, 1, 2, 12, 0, 0, TimeSpan.Zero),
        BusinessDate = new DateOnly(2026, 1, 2),
        EstimatedTime = EstimatedTime.Between15And30Minutes,
        FrictionLevel = FrictionLevel.Low
    };

    private static AwardQuestXpDto InvalidAward(
        Guid? sourceEntityId = null,
        EstimatedTime? estimatedTime = null,
        FrictionLevel? frictionLevel = null)
    {
        var award = CreateAward();
        award.SourceEntityId = sourceEntityId ?? award.SourceEntityId;
        award.EstimatedTime = estimatedTime ?? award.EstimatedTime;
        award.FrictionLevel = frictionLevel ?? award.FrictionLevel;
        return award;
    }

    private static UserProgression NewProgression() => new()
    {
        UserId = UserId, CurrentLevel = 1, CurrentEchelon = Echelon.Iron,
        DailyQuestXpToday = 0, TotalLifetimeXp = 0, Version = 0
    };
}
