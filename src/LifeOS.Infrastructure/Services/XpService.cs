using LifeOS.Core.Abstractions;
using LifeOS.Core.Constants;
using LifeOS.Core.DTOs.Xp;
using LifeOS.Core.Entities;
using LifeOS.Core.Enums.Xp;
using LifeOS.Core.Exceptions;
using LifeOS.Core.Mappings;
using LifeOS.Core.Progression;
using LifeOS.Core.Services;
using Microsoft.Extensions.Logging;

namespace LifeOS.Infrastructure.Services;

public sealed class XpService : IXpService
{
    private const int MaxAttempts = 3;

    private readonly IXpRepository _repository;
    private readonly ICurrentUserService _currentUser;
    private readonly IUserSettingsService _userSettings;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<XpService> _logger;

    public XpService(IXpRepository repository, ICurrentUserService currentUser,
        IUserSettingsService userSettings, IDateTimeProvider dateTimeProvider,
        ILogger<XpService> logger)
    {
        _repository = repository;
        _currentUser = currentUser;
        _userSettings = userSettings;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    public async Task<XpAwardResultDto> AwardQuestXpAsync(AwardQuestXpDto dto,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        ValidateAward(dto);
        var key = dto.SourceType == XpSourceType.Task
            ? XpIdempotencyKeyFactory.ForTask(dto.SourceEntityId)
            : XpIdempotencyKeyFactory.ForHabit(dto.SourceEntityId, dto.BusinessDate);
        var rawXp = XpRules.CalculateQuestXp(dto.EstimatedTime, dto.FrictionLevel);

        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            var duplicate = await _repository.FindByIdempotencyKeyAsync(userId, key, cancellationToken);
            var progression = await _repository.GetOrCreateProgressionAsync(userId, cancellationToken);
            if (duplicate is not null)
            {
                return CreateDuplicateResult(rawXp, duplicate, progression);
            }

            var alreadyAwarded = await _repository.GetQuestXpSumAsync(userId, dto.BusinessDate, cancellationToken);
            var awardedXp = XpRules.CalculateActualQuestXp(alreadyAwarded, rawXp);
            var currentDailyXp = checked(alreadyAwarded + awardedXp);
            if (awardedXp == 0)
            {
                return CreateAwardResult(rawXp, 0, true, null, progression,
                    progression.CurrentLevel, progression.CurrentEchelon);
            }

            var resultingLifetimeXp = checked(progression.TotalLifetimeXp + awardedXp);
            var resultingLevel = XpRules.CalculateLevel(resultingLifetimeXp);
            var resultingEchelon = XpRules.CalculateEchelon(resultingLevel);
            var resultingVersion = checked(progression.Version + 1);
            var commit = await _repository.CommitAwardAsync(new XpAwardCommitRequest
            {
                UserId = userId,
                Source = XpSource.QuestCompletion,
                SourceType = dto.SourceType,
                SourceEntityId = dto.SourceEntityId,
                XpAmount = awardedXp,
                OccurredAtUtc = dto.OccurredAtUtc,
                BusinessDate = dto.BusinessDate,
                IdempotencyKey = key,
                ExpectedVersion = progression.Version,
                ResultingTotalLifetimeXp = resultingLifetimeXp,
                ResultingCurrentLevel = resultingLevel,
                ResultingCurrentEchelon = resultingEchelon,
                ResultingDailyQuestXpToday = Math.Min(currentDailyXp, XpConstants.DailyQuestXpCap),
                ResultingDailyQuestXpDate = dto.BusinessDate,
                ResultingVersion = resultingVersion
            }, cancellationToken);

            if (commit.Status == XpAwardCommitStatus.Committed)
            {
                var committedProgression = commit.Progression ?? throw new InvalidOperationException(
                    "A committed XP award did not return progression.");
                if (progression.CurrentLevel != resultingLevel || progression.CurrentEchelon != resultingEchelon)
                {
                    _logger.LogInformation("User {UserId} progressed to level {Level} and echelon {Echelon}",
                        userId, resultingLevel, resultingEchelon);
                }
                return CreateAwardResult(rawXp, awardedXp, awardedXp < rawXp, commit.Transaction?.Id,
                    committedProgression, progression.CurrentLevel, progression.CurrentEchelon);
            }

            if (commit.Status == XpAwardCommitStatus.Duplicate)
            {
                var authoritative = commit.Progression ?? await _repository.GetOrCreateProgressionAsync(
                    userId, cancellationToken);
                return CreateDuplicateResult(rawXp, commit.Transaction, authoritative);
            }

            if (commit.Status != XpAwardCommitStatus.ConcurrencyConflict)
            {
                throw new InvalidOperationException("The XP repository returned an unknown commit status.");
            }

            if (attempt == MaxAttempts)
            {
                throw new XpConcurrencyException();
            }
        }

        throw new InvalidOperationException("The XP award could not be committed.");
    }

    public async Task<UserProgressionDto> GetProgressionAsync(CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        var progression = await _repository.GetOrCreateProgressionAsync(userId, cancellationToken);
        var settings = await _userSettings.GetCurrentUserSettingsAsync(cancellationToken);
        var currentDate = _dateTimeProvider.GetCurrentDate(settings.TimeZoneId);
        var dailyXp = await _repository.GetQuestXpSumAsync(userId, currentDate, cancellationToken);
        return new UserProgressionDto
        {
            TotalLifetimeXp = progression.TotalLifetimeXp,
            CurrentLevel = progression.CurrentLevel,
            CurrentEchelon = progression.CurrentEchelon,
            DailyQuestXpToday = dailyXp,
            DailyQuestXpDate = currentDate
        };
    }

    public async Task<IReadOnlyList<XpTransactionDto>> GetXpHistoryAsync(CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        var history = await _repository.GetHistoryAsync(userId, cancellationToken);
        return history.Select(transaction => transaction.ToDto()).ToList();
    }

    private Guid GetCurrentUserId()
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId == Guid.Empty)
        {
            throw new CurrentUserUnavailableException();
        }
        return _currentUser.UserId;
    }

    private static void ValidateAward(AwardQuestXpDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        if (!Enum.IsDefined(dto.SourceType) ||
            (dto.SourceType != XpSourceType.Task && dto.SourceType != XpSourceType.Habit))
        {
            throw new ValidationException("The XP source type is not supported for Quest completion.");
        }
        if (dto.SourceEntityId == Guid.Empty)
        {
            throw new ValidationException("The XP source entity is required.");
        }
        if (!Enum.IsDefined(dto.EstimatedTime) || !Enum.IsDefined(dto.FrictionLevel))
        {
            throw new ValidationException("The XP award metadata is invalid.");
        }
        if (dto.OccurredAtUtc == DateTimeOffset.MinValue || dto.OccurredAtUtc == DateTimeOffset.MaxValue)
        {
            throw new ValidationException("The occurrence timestamp is invalid.");
        }
    }

    private static XpAwardResultDto CreateDuplicateResult(int rawXp, XpTransaction? transaction,
        UserProgression progression)
    {
        return CreateAwardResult(rawXp, 0, false, transaction?.Id,
            progression, progression.CurrentLevel, progression.CurrentEchelon, true);
    }

    private static XpAwardResultDto CreateAwardResult(int rawXp, int awardedXp, bool capConstrained,
        Guid? transactionId, UserProgression progression, int previousLevel, Echelon previousEchelon,
        bool duplicate = false)
    {
        return new XpAwardResultDto
        {
            RawXp = rawXp,
            AwardedXp = awardedXp,
            IsDuplicate = duplicate,
            IsCapConstrained = capConstrained,
            TransactionId = transactionId,
            Progression = progression.ToDto(),
            PreviousLevel = previousLevel,
            CurrentLevel = progression.CurrentLevel,
            PreviousEchelon = previousEchelon,
            CurrentEchelon = progression.CurrentEchelon
        };
    }
}
