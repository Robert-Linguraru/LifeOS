using LifeOS.Core.Abstractions;
using LifeOS.Core.DTOs.Xp;
using LifeOS.Core.Entities;
using LifeOS.Core.Enums;
using LifeOS.Core.Enums.Notifications;
using LifeOS.Core.Enums.Xp;
using LifeOS.Core.Services;
using LifeOS.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace LifeOS.Tests.Services;

public sealed class XpNotificationServiceTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private readonly Mock<IXpRepository> _repository = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly Mock<IUserSettingsService> _settings = new();
    private readonly Mock<IDateTimeProvider> _dateTime = new();

    [Fact]
    public async Task NoTransition_SendsNoNotificationDrafts()
    {
        var progression = Progression(0, 1, Echelon.Iron, 0);
        SetupAward(progression, new UserProgression
        {
            UserId = UserId,
            TotalLifetimeXp = 100,
            CurrentLevel = 1,
            CurrentEchelon = Echelon.Iron,
            Version = 1
        });

        await CreateService().AwardQuestXpAsync(CreateAward());

        _repository.Verify(repository => repository.CommitAwardAsync(
            It.Is<XpAwardCommitRequest>(request => request.Notifications.Count == 0),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LevelTransition_SendsOneStableLevelDraft()
    {
        var progression = Progression(170, 1, Echelon.Iron, 0);
        SetupAward(progression, Progression(270, 2, Echelon.Iron, 1));

        await CreateService().AwardQuestXpAsync(CreateAward());
        var request = CapturedRequest();

        var draft = Assert.Single(request.Notifications);
        Assert.Equal(NotificationType.LevelUp, draft.Type);
        Assert.Equal(NotificationSourceType.XpTransaction, draft.SourceType);
        Assert.Equal(request.XpTransactionId, draft.SourceId);
        Assert.Equal($"XpLevelUp:{request.XpTransactionId:N}", draft.IdempotencyKey);
        Assert.Equal("You reached level 2.", draft.Message);
    }

    [Fact]
    public async Task CombinedTransition_SendsTwoDistinctDrafts()
    {
        var progression = Progression(2500, 9, Echelon.Iron, 0);
        SetupAward(progression, Progression(2700, 10, Echelon.Bronze, 1), rawXp: 200);

        await CreateService().AwardQuestXpAsync(CreateAward(EstimatedTime.Over60Minutes));
        var request = CapturedRequest();

        Assert.Equal(2, request.Notifications.Count);
        Assert.Equal(2, request.Notifications.Select(draft => draft.NotificationId).Distinct().Count());
        Assert.Equal(2, request.Notifications.Select(draft => draft.IdempotencyKey).Distinct().Count());
        Assert.Contains(request.Notifications, draft => draft.Type == NotificationType.LevelUp);
        Assert.Contains(request.Notifications, draft => draft.Type == NotificationType.EchelonChanged);
    }

    [Fact]
    public async Task DuplicatePrecheck_SendsNoCommitDrafts()
    {
        var transaction = new XpTransaction { Id = Guid.NewGuid(), XpAmount = 100 };
        _currentUser.SetupGet(user => user.IsAuthenticated).Returns(true);
        _currentUser.SetupGet(user => user.UserId).Returns(UserId);
        _repository.Setup(repository => repository.FindByIdempotencyKeyAsync(
                UserId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction);
        _repository.Setup(repository => repository.GetOrCreateProgressionAsync(
                UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Progression(270, 2, Echelon.Iron, 1));

        var result = await CreateService().AwardQuestXpAsync(CreateAward());

        Assert.True(result.IsDuplicate);
        _repository.Verify(repository => repository.CommitAwardAsync(
            It.IsAny<XpAwardCommitRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CapZero_SendsNoNotificationDrafts()
    {
        _currentUser.SetupGet(user => user.IsAuthenticated).Returns(true);
        _currentUser.SetupGet(user => user.UserId).Returns(UserId);
        _repository.Setup(repository => repository.FindByIdempotencyKeyAsync(
                UserId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((XpTransaction?)null);
        _repository.Setup(repository => repository.GetOrCreateProgressionAsync(
                UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Progression(2500, 10, Echelon.Bronze, 0));
        _repository.Setup(repository => repository.GetQuestXpSumAsync(
                UserId, It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(500);

        await CreateService().AwardQuestXpAsync(CreateAward());

        _repository.Verify(repository => repository.CommitAwardAsync(
            It.IsAny<XpAwardCommitRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private XpService CreateService()
    {
        _currentUser.SetupGet(user => user.IsAuthenticated).Returns(true);
        _currentUser.SetupGet(user => user.UserId).Returns(UserId);
        return new XpService(
            _repository.Object,
            _currentUser.Object,
            _settings.Object,
            _dateTime.Object,
            NullLogger<XpService>.Instance);
    }

    private void SetupAward(
        UserProgression progression,
        UserProgression resulting,
        int rawXp = 100)
    {
        _repository.Setup(repository => repository.FindByIdempotencyKeyAsync(
                UserId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((XpTransaction?)null);
        _repository.Setup(repository => repository.GetOrCreateProgressionAsync(
                UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(progression);
        _repository.Setup(repository => repository.GetQuestXpSumAsync(
                UserId, It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        _repository.Setup(repository => repository.CommitAwardAsync(
                It.IsAny<XpAwardCommitRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new XpAwardCommitResult
            {
                Status = XpAwardCommitStatus.Committed,
                Transaction = new XpTransaction { Id = Guid.NewGuid(), XpAmount = rawXp },
                Progression = resulting
            });
    }

    private XpAwardCommitRequest CapturedRequest()
    {
        var invocation = _repository.Invocations.Single(item =>
            item.Method.Name == nameof(IXpRepository.CommitAwardAsync));
        return (XpAwardCommitRequest)invocation.Arguments[0]!;
    }

    private static AwardQuestXpDto CreateAward(
        EstimatedTime estimatedTime = EstimatedTime.Between15And30Minutes) => new()
    {
        SourceType = XpSourceType.Task,
        SourceEntityId = Guid.NewGuid(),
        OccurredAtUtc = new DateTimeOffset(2026, 8, 20, 12, 0, 0, TimeSpan.Zero),
        BusinessDate = new DateOnly(2026, 8, 20),
        EstimatedTime = estimatedTime,
        FrictionLevel = FrictionLevel.Low
    };

    private static UserProgression Progression(
        long totalXp,
        int level,
        Echelon echelon,
        long version) => new()
    {
        UserId = UserId,
        TotalLifetimeXp = totalXp,
        CurrentLevel = level,
        CurrentEchelon = echelon,
        Version = version
    };
}
