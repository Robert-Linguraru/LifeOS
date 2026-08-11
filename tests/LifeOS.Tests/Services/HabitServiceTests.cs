using LifeOS.Core.Abstractions;
using LifeOS.Core.Abstractions.Habits;
using LifeOS.Core.DTOs;
using LifeOS.Core.DTOs.Habits;
using LifeOS.Core.Entities;
using LifeOS.Core.Enums;
using LifeOS.Core.Enums.Habits;
using LifeOS.Core.Exceptions;
using LifeOS.Core.Services;
using LifeOS.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace LifeOS.Tests.Services;

public sealed class HabitServiceTests
{
    private static readonly Guid UserId = Guid.NewGuid();

    private readonly Mock<IHabitRepository> _repository = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly Mock<IUserSettingsService> _userSettingsService = new();
    private readonly Mock<IDateTimeProvider> _dateTimeProvider = new();
    private readonly Mock<ILogger<HabitService>> _logger = new();

    private HabitService CreateService()
    {
        _currentUser
            .Setup(user => user.UserId)
            .Returns(UserId);

        _currentUser
            .Setup(user => user.IsAuthenticated)
            .Returns(true);

        _userSettingsService
            .Setup(service => service.GetCurrentUserSettingsAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserSettingsDto
            {
                UserId = UserId,
                TimeZoneId = "UTC"
            });

        _dateTimeProvider
            .Setup(provider => provider.UtcNow)
            .Returns(new DateTimeOffset(
                2026,
                8,
                10,
                12,
                0,
                0,
                TimeSpan.Zero));

        _dateTimeProvider
            .Setup(provider => provider.GetCurrentDate("UTC"))
            .Returns(new DateOnly(2026, 8, 10));

        return new HabitService(
            _repository.Object,
            _currentUser.Object,
            _userSettingsService.Object,
            _dateTimeProvider.Object,
            _logger.Object);
    }

    [Fact]
    public async Task CreateHabitAsync_BinaryHabit_NormalizesAndPersists()
    {
        var service = CreateService();

        var result = await service.CreateHabitAsync(
            new CreateHabitDto
            {
                Name = "  Read  ",
                Description = "   ",
                TargetType = HabitTargetType.Binary,
                TargetQuantity = 30m,
                TargetUnit = "minutes"
            });

        Assert.Equal("Read", result.Name);
        Assert.Null(result.Description);
        Assert.Equal(HabitTargetType.Binary, result.TargetType);
        Assert.Null(result.TargetQuantity);
        Assert.Null(result.TargetUnit);
        Assert.True(result.IsActive);

        _repository.Verify(
            repository => repository.AddAsync(
                It.Is<Habit>(habit =>
                    habit.UserId == UserId &&
                    habit.Name == "Read" &&
                    habit.Description == null &&
                    habit.IsActive &&
                    habit.TargetQuantity == null &&
                    habit.TargetUnit == null),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateHabitAsync_QuantityHabit_NormalizesAndPersistsMetadata()
    {
        var service = CreateService();

        var result = await service.CreateHabitAsync(
            new CreateHabitDto
            {
                Name = "Exercise",
                TargetType = HabitTargetType.Quantity,
                TargetQuantity = 30.50m,
                TargetUnit = " minutes "
            });

        Assert.Equal(HabitTargetType.Quantity, result.TargetType);
        Assert.Equal(30.50m, result.TargetQuantity);
        Assert.Equal("minutes", result.TargetUnit);

        _repository.Verify(
            repository => repository.AddAsync(
                It.Is<Habit>(habit =>
                    habit.UserId == UserId &&
                    habit.TargetQuantity == 30.50m &&
                    habit.TargetUnit == "minutes"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, true)]
    public async Task CreateHabitAsync_InvalidCurrentUser_ThrowsWithoutRepositoryAccess(
        bool isAuthenticated,
        bool emptyUserId)
    {
        var service = CreateService();
        ConfigureCurrentUser(isAuthenticated, emptyUserId);

        await Assert.ThrowsAsync<CurrentUserUnavailableException>(
            () => service.CreateHabitAsync(CreateHabitDto()));

        VerifyRepositoryNotCalled();
    }

    [Fact]
    public async Task CreateHabitAsync_BlankName_ThrowsValidationException()
    {
        await AssertInvalidCreateAsync(new CreateHabitDto { Name = "   " });
    }

    [Fact]
    public async Task CreateHabitAsync_NameTooLong_ThrowsValidationException()
    {
        await AssertInvalidCreateAsync(
            new CreateHabitDto { Name = new string('a', 201) });
    }

    [Fact]
    public async Task CreateHabitAsync_DescriptionTooLong_ThrowsValidationException()
    {
        await AssertInvalidCreateAsync(
            new CreateHabitDto
            {
                Name = "Read",
                Description = new string('a', 2001)
            });
    }

    [Theory]
    [InlineData(HabitFrequency.SelectedDays)]
    [InlineData(HabitFrequency.Weekly)]
    [InlineData(HabitFrequency.Monthly)]
    [InlineData((HabitFrequency)999)]
    public async Task CreateHabitAsync_NonDailyFrequency_ThrowsValidationException(
        HabitFrequency frequency)
    {
        await AssertInvalidCreateAsync(
            new CreateHabitDto
            {
                Name = "Read",
                Frequency = frequency
            });
    }

    [Fact]
    public async Task CreateHabitAsync_InvalidTargetType_ThrowsValidationException()
    {
        await AssertInvalidCreateAsync(
            new CreateHabitDto
            {
                Name = "Read",
                TargetType = (HabitTargetType)999
            });
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    public async Task CreateHabitAsync_InvalidQuantity_ThrowsValidationException(
        string quantity)
    {
        await AssertInvalidCreateAsync(
            new CreateHabitDto
            {
                Name = "Exercise",
                TargetType = HabitTargetType.Quantity,
                TargetQuantity = decimal.Parse(quantity),
                TargetUnit = "minutes"
            });
    }

    [Fact]
    public async Task CreateHabitAsync_MissingQuantity_ThrowsValidationException()
    {
        await AssertInvalidCreateAsync(
            new CreateHabitDto
            {
                Name = "Exercise",
                TargetType = HabitTargetType.Quantity,
                TargetUnit = "minutes"
            });
    }

    [Fact]
    public async Task CreateHabitAsync_QuantityWithoutUnit_ThrowsValidationException()
    {
        await AssertInvalidCreateAsync(
            new CreateHabitDto
            {
                Name = "Exercise",
                TargetType = HabitTargetType.Quantity,
                TargetQuantity = 30m
            });
    }

    [Fact]
    public async Task CreateHabitAsync_TargetUnitTooLong_ThrowsValidationException()
    {
        await AssertInvalidCreateAsync(
            new CreateHabitDto
            {
                Name = "Exercise",
                TargetType = HabitTargetType.Quantity,
                TargetQuantity = 30m,
                TargetUnit = new string('a', 51)
            });
    }

    [Fact]
    public async Task CreateHabitAsync_InvalidEstimatedTime_ThrowsValidationException()
    {
        await AssertInvalidCreateAsync(
            new CreateHabitDto
            {
                Name = "Read",
                EstimatedTime = (EstimatedTime)999
            });
    }

    [Fact]
    public async Task CreateHabitAsync_InvalidFrictionLevel_ThrowsValidationException()
    {
        await AssertInvalidCreateAsync(
            new CreateHabitDto
            {
                Name = "Read",
                FrictionLevel = (FrictionLevel)999
            });
    }

    [Fact]
    public async Task UpdateHabitAsync_ActiveHabit_NormalizesAndPreservesOwnershipAndLifecycle()
    {
        var habitId = Guid.NewGuid();
        var habit = new Habit
        {
            Id = habitId,
            UserId = UserId,
            Name = "Old name",
            IsActive = true
        };

        _repository
            .Setup(repository => repository.GetByIdAsync(
                UserId,
                habitId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(habit);

        var service = CreateService();

        var result = await service.UpdateHabitAsync(
            habitId,
            new UpdateHabitDto
            {
                Name = "  New name  ",
                Description = "  New description  ",
                TargetType = HabitTargetType.Quantity,
                TargetQuantity = 2m,
                TargetUnit = "  times  "
            });

        Assert.Equal("New name", result.Name);
        Assert.Equal("New description", result.Description);
        Assert.Equal(HabitTargetType.Quantity, result.TargetType);
        Assert.Equal(2m, result.TargetQuantity);
        Assert.Equal("times", result.TargetUnit);
        Assert.True(result.IsActive);
        Assert.Equal(UserId, habit.UserId);
        Assert.True(habit.IsActive);

        _repository.Verify(
            repository => repository.UpdateAsync(
                habit,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateHabitAsync_MissingHabit_ThrowsResourceNotFoundException()
    {
        var habitId = Guid.NewGuid();

        _repository
            .Setup(repository => repository.GetByIdAsync(
                UserId,
                habitId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Habit?)null);

        var service = CreateService();

        await Assert.ThrowsAsync<ResourceNotFoundException>(
            () => service.UpdateHabitAsync(
                habitId,
                new UpdateHabitDto { Name = "Updated" }));

        _repository.Verify(
            repository => repository.UpdateAsync(
                It.IsAny<Habit>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _repository.Verify(
            repository => repository.GetCompletionDatesByUserIdAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateHabitAsync_ArchivedHabit_ThrowsValidationException()
    {
        var habitId = Guid.NewGuid();
        var habit = new Habit
        {
            Id = habitId,
            UserId = UserId,
            IsActive = false
        };

        _repository
            .Setup(repository => repository.GetByIdAsync(
                UserId,
                habitId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(habit);

        var service = CreateService();

        await Assert.ThrowsAsync<ValidationException>(
            () => service.UpdateHabitAsync(
                habitId,
                new UpdateHabitDto { Name = "Updated" }));

        _repository.Verify(
            repository => repository.UpdateAsync(
                It.IsAny<Habit>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, true)]
    public async Task UpdateHabitAsync_InvalidCurrentUser_ThrowsWithoutRepositoryAccess(
        bool isAuthenticated,
        bool emptyUserId)
    {
        var service = CreateService();
        ConfigureCurrentUser(isAuthenticated, emptyUserId);

        await Assert.ThrowsAsync<CurrentUserUnavailableException>(
            () => service.UpdateHabitAsync(
                Guid.NewGuid(),
                new UpdateHabitDto { Name = "Updated" }));

        VerifyRepositoryNotCalled();
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, true)]
    public async Task GetHabitByIdAsync_InvalidCurrentUser_ThrowsWithoutRepositoryAccess(
        bool isAuthenticated,
        bool emptyUserId)
    {
        var service = CreateService();
        ConfigureCurrentUser(isAuthenticated, emptyUserId);

        await Assert.ThrowsAsync<CurrentUserUnavailableException>(
            () => service.GetHabitByIdAsync(Guid.NewGuid()));

        VerifyRepositoryNotCalled();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task GetHabitByIdAsync_ReturnsActiveOrArchivedHabit(
        bool isActive)
    {
        var habitId = Guid.NewGuid();
        var habit = new Habit
        {
            Id = habitId,
            UserId = UserId,
            Name = "Read",
            IsActive = isActive
        };

        _repository
            .Setup(repository => repository.GetByIdAsync(
                UserId,
                habitId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(habit);

        var service = CreateService();

        var result = await service.GetHabitByIdAsync(habitId);

        Assert.Equal(habitId, result.Id);
        Assert.Equal(isActive, result.IsActive);
    }

    [Fact]
    public async Task GetHabitByIdAsync_MissingHabit_ThrowsResourceNotFoundException()
    {
        var habitId = Guid.NewGuid();

        _repository
            .Setup(repository => repository.GetByIdAsync(
                UserId,
                habitId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Habit?)null);

        var service = CreateService();

        await Assert.ThrowsAsync<ResourceNotFoundException>(
            () => service.GetHabitByIdAsync(habitId));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, true)]
    public async Task ArchiveHabitAsync_InvalidCurrentUser_ThrowsWithoutRepositoryAccess(
        bool isAuthenticated,
        bool emptyUserId)
    {
        var service = CreateService();
        ConfigureCurrentUser(isAuthenticated, emptyUserId);

        await Assert.ThrowsAsync<CurrentUserUnavailableException>(
            () => service.ArchiveHabitAsync(Guid.NewGuid()));

        VerifyRepositoryNotCalled();
    }

    [Fact]
    public async Task ArchiveHabitAsync_ActiveHabit_SetsInactiveAndPersists()
    {
        var habitId = Guid.NewGuid();
        var habit = new Habit
        {
            Id = habitId,
            UserId = UserId,
            IsActive = true
        };

        _repository
            .Setup(repository => repository.GetByIdAsync(
                UserId,
                habitId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(habit);

        var service = CreateService();

        var result = await service.ArchiveHabitAsync(habitId);

        Assert.False(result.IsActive);
        Assert.False(habit.IsActive);

        _repository.Verify(
            repository => repository.UpdateAsync(
                habit,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ArchiveHabitAsync_AlreadyArchived_IsIdempotent()
    {
        var habitId = Guid.NewGuid();
        var habit = new Habit
        {
            Id = habitId,
            UserId = UserId,
            IsActive = false
        };

        _repository
            .Setup(repository => repository.GetByIdAsync(
                UserId,
                habitId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(habit);

        var service = CreateService();

        var result = await service.ArchiveHabitAsync(habitId);

        Assert.False(result.IsActive);

        _repository.Verify(
            repository => repository.UpdateAsync(
                It.IsAny<Habit>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ArchiveHabitAsync_MissingHabit_ThrowsResourceNotFoundException()
    {
        var habitId = Guid.NewGuid();

        _repository
            .Setup(repository => repository.GetByIdAsync(
                UserId,
                habitId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Habit?)null);

        var service = CreateService();

        await Assert.ThrowsAsync<ResourceNotFoundException>(
            () => service.ArchiveHabitAsync(habitId));

        _repository.Verify(
            repository => repository.UpdateAsync(
                It.IsAny<Habit>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, true)]
    public async Task GetHabitListAsync_InvalidCurrentUser_ThrowsWithoutRepositoryAccess(
        bool isAuthenticated,
        bool emptyUserId)
    {
        var service = CreateService();
        ConfigureCurrentUser(isAuthenticated, emptyUserId);

        await Assert.ThrowsAsync<CurrentUserUnavailableException>(
            () => service.GetHabitListAsync());

        VerifyRepositoryNotCalled();
    }

    [Fact]
    public async Task GetHabitListAsync_SeparatesAndSortsHabitState()
    {
        var currentDate = new DateOnly(2026, 8, 10);
        var incomplete = CreateHabit("Alpha", true);
        var completed = CreateHabit("Beta", true);
        var archived = CreateHabit("Archived", false);
        archived.UpdatedAtUtc = new DateTimeOffset(
            2026,
            8,
            9,
            12,
            0,
            0,
            TimeSpan.Zero);

        _repository
            .Setup(repository => repository.GetAllByUserIdAsync(
                UserId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([incomplete, completed, archived]);

        _repository
            .Setup(repository => repository.GetCompletionDatesByUserIdAsync(
                UserId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                (completed.Id, currentDate),
                (completed.Id, currentDate.AddDays(-1))
            ]);

        var service = CreateService();
        SetupDate(currentDate);

        var result = await service.GetHabitListAsync();

        Assert.Equal(currentDate, result.CurrentDate);
        Assert.Equal(2, result.Active.Count);
        Assert.Single(result.Archived);
        Assert.Equal(incomplete.Id, result.Active[0].Id);
        Assert.False(result.Active[0].IsCompletedToday);
        Assert.Equal(completed.Id, result.Active[1].Id);
        Assert.True(result.Active[1].IsCompletedToday);
        Assert.Equal(2, result.Active[1].CurrentStreak);
        Assert.Equal(archived.Id, result.Archived[0].Id);
        Assert.False(result.Archived[0].IsCompletedToday);
        Assert.Equal(0, result.Archived[0].CurrentStreak);
    }

    [Fact]
    public async Task GetHabitListAsync_CurrentStreakUsesApprovedRules()
    {
        var currentDate = new DateOnly(2026, 8, 10);
        var habits = new[]
        {
            CreateHabit("Today", true),
            CreateHabit("Consecutive", true),
            CreateHabit("Yesterday", true),
            CreateHabit("Neither", true),
            CreateHabit("Gap", true),
            CreateHabit("Future", true),
            CreateHabit("Duplicates", true)
        };

        _repository
            .Setup(repository => repository.GetAllByUserIdAsync(
                UserId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(habits);

        _repository
            .Setup(repository => repository.GetCompletionDatesByUserIdAsync(
                UserId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                (habits[0].Id, currentDate),
                (habits[1].Id, currentDate.AddDays(-2)),
                (habits[1].Id, currentDate),
                (habits[1].Id, currentDate.AddDays(-1)),
                (habits[2].Id, currentDate.AddDays(-2)),
                (habits[2].Id, currentDate.AddDays(-1)),
                (habits[4].Id, currentDate),
                (habits[4].Id, currentDate.AddDays(-2)),
                (habits[5].Id, currentDate.AddDays(1)),
                (habits[6].Id, currentDate.AddDays(-1)),
                (habits[6].Id, currentDate),
                (habits[6].Id, currentDate.AddDays(-1))
            ]);

        var service = CreateService();
        SetupDate(currentDate);

        var result = await service.GetHabitListAsync();
        var summaries = result.Active.ToDictionary(summary => summary.Name);

        Assert.Equal(1, summaries["Today"].CurrentStreak);
        Assert.Equal(3, summaries["Consecutive"].CurrentStreak);
        Assert.Equal(2, summaries["Yesterday"].CurrentStreak);
        Assert.Equal(0, summaries["Neither"].CurrentStreak);
        Assert.Equal(1, summaries["Gap"].CurrentStreak);
        Assert.Equal(0, summaries["Future"].CurrentStreak);
        Assert.Equal(2, summaries["Duplicates"].CurrentStreak);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, true)]
    public async Task CompleteHabitAsync_InvalidCurrentUser_ThrowsWithoutRepositoryAccess(
        bool isAuthenticated,
        bool emptyUserId)
    {
        var service = CreateService();
        ConfigureCurrentUser(isAuthenticated, emptyUserId);

        await Assert.ThrowsAsync<CurrentUserUnavailableException>(
            () => service.CompleteHabitAsync(Guid.NewGuid()));

        VerifyRepositoryNotCalled();
    }

    [Fact]
    public async Task CompleteHabitAsync_FirstCompletionCreatesLogAndReturnsStreak()
    {
        var currentDate = new DateOnly(2026, 8, 10);
        var habit = CreateHabit("Read", true);
        var utcNow = new DateTimeOffset(
            2026,
            8,
            10,
            23,
            30,
            0,
            TimeSpan.Zero);

        _repository
            .Setup(repository => repository.GetByIdAsync(
                UserId,
                habit.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(habit);
        _repository
            .Setup(repository => repository.GetLogByDateAsync(
                UserId,
                habit.Id,
                currentDate,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((HabitLog?)null);
        _repository
            .Setup(repository => repository.TryAddLogAsync(
                It.IsAny<HabitLog>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _repository
            .Setup(repository => repository.GetCompletionDatesAsync(
                UserId,
                habit.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([currentDate, currentDate.AddDays(-1)]);

        var service = CreateService();
        SetupDate(currentDate, utcNow);

        var result = await service.CompleteHabitAsync(habit.Id);

        Assert.True(result.IsCompletedToday);
        Assert.Equal(2, result.CurrentStreak);
        _repository.Verify(
            repository => repository.TryAddLogAsync(
                It.Is<HabitLog>(log =>
                    log.UserId == UserId &&
                    log.HabitId == habit.Id &&
                    log.CompletionDate == currentDate &&
                    log.CompletedAtUtc == utcNow),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CompleteHabitAsync_ExistingLogIsSuccessfulNoOp()
    {
        var currentDate = new DateOnly(2026, 8, 10);
        var habit = CreateHabit("Read", true);
        var existingLog = new HabitLog
        {
            UserId = UserId,
            HabitId = habit.Id,
            CompletionDate = currentDate,
            CompletedAtUtc = new DateTimeOffset(
                2026,
                8,
                10,
                8,
                0,
                0,
                TimeSpan.Zero)
        };

        SetupHabitLookup(habit);
        _repository
            .Setup(repository => repository.GetLogByDateAsync(
                UserId,
                habit.Id,
                currentDate,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingLog);
        _repository
            .Setup(repository => repository.GetCompletionDatesAsync(
                UserId,
                habit.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([currentDate]);

        var service = CreateService();
        SetupDate(currentDate);

        var result = await service.CompleteHabitAsync(habit.Id);

        Assert.True(result.IsCompletedToday);
        _repository.Verify(
            repository => repository.TryAddLogAsync(
                It.IsAny<HabitLog>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CompleteHabitAsync_ConcurrentDuplicateRereadsAuthoritativeLog()
    {
        var currentDate = new DateOnly(2026, 8, 10);
        var habit = CreateHabit("Read", true);
        var authoritativeLog = new HabitLog
        {
            UserId = UserId,
            HabitId = habit.Id,
            CompletionDate = currentDate,
            CompletedAtUtc = new DateTimeOffset(
                2026,
                8,
                10,
                8,
                0,
                0,
                TimeSpan.Zero)
        };

        SetupHabitLookup(habit);
        _repository
            .SetupSequence(repository => repository.GetLogByDateAsync(
                UserId,
                habit.Id,
                currentDate,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((HabitLog?)null)
            .ReturnsAsync(authoritativeLog);
        _repository
            .Setup(repository => repository.TryAddLogAsync(
                It.IsAny<HabitLog>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _repository
            .Setup(repository => repository.GetCompletionDatesAsync(
                UserId,
                habit.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([currentDate]);

        var service = CreateService();
        SetupDate(currentDate);

        var result = await service.CompleteHabitAsync(habit.Id);

        Assert.True(result.IsCompletedToday);
        _repository.Verify(
            repository => repository.GetLogByDateAsync(
                UserId,
                habit.Id,
                currentDate,
                It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task CompleteHabitAsync_ArchivedHabit_ThrowsWithoutInsert()
    {
        var habit = CreateHabit("Read", false);
        SetupHabitLookup(habit);

        var service = CreateService();

        await Assert.ThrowsAsync<ValidationException>(
            () => service.CompleteHabitAsync(habit.Id));

        _repository.Verify(
            repository => repository.TryAddLogAsync(
                It.IsAny<HabitLog>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CompleteHabitAsync_MissingHabit_ThrowsResourceNotFoundException()
    {
        var habitId = Guid.NewGuid();
        _repository
            .Setup(repository => repository.GetByIdAsync(
                UserId,
                habitId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Habit?)null);

        var service = CreateService();

        await Assert.ThrowsAsync<ResourceNotFoundException>(
            () => service.CompleteHabitAsync(habitId));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, true)]
    public async Task GetHabitHistoryAsync_InvalidCurrentUser_ThrowsWithoutRepositoryAccess(
        bool isAuthenticated,
        bool emptyUserId)
    {
        var service = CreateService();
        ConfigureCurrentUser(isAuthenticated, emptyUserId);

        await Assert.ThrowsAsync<CurrentUserUnavailableException>(
            () => service.GetHabitHistoryAsync(Guid.NewGuid()));

        VerifyRepositoryNotCalled();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task GetHabitHistoryAsync_ReturnsActiveOrArchivedHistory(
        bool isActive)
    {
        var habit = CreateHabit("Read", isActive);
        var older = new HabitLog
        {
            HabitId = habit.Id,
            CompletionDate = new DateOnly(2026, 8, 8),
            CompletedAtUtc = new DateTimeOffset(
                2026,
                8,
                8,
                8,
                0,
                0,
                TimeSpan.Zero)
        };
        var newer = new HabitLog
        {
            HabitId = habit.Id,
            CompletionDate = new DateOnly(2026, 8, 10),
            CompletedAtUtc = new DateTimeOffset(
                2026,
                8,
                10,
                8,
                0,
                0,
                TimeSpan.Zero)
        };

        SetupHabitLookup(habit);
        _repository
            .Setup(repository => repository.GetLogsByHabitIdAsync(
                UserId,
                habit.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([newer, older]);

        var service = CreateService();

        var result = await service.GetHabitHistoryAsync(habit.Id);

        Assert.Equal(habit.Id, result.HabitId);
        Assert.Equal(isActive, result.IsActive);
        Assert.Equal(2, result.Entries.Count);
        Assert.Equal(newer.CompletionDate, result.Entries[0].CompletionDate);
        Assert.Equal(older.CompletionDate, result.Entries[1].CompletionDate);
    }

    [Fact]
    public async Task GetHabitHistoryAsync_MissingHabit_ThrowsResourceNotFoundException()
    {
        var habitId = Guid.NewGuid();
        _repository
            .Setup(repository => repository.GetByIdAsync(
                UserId,
                habitId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Habit?)null);

        var service = CreateService();

        await Assert.ThrowsAsync<ResourceNotFoundException>(
            () => service.GetHabitHistoryAsync(habitId));
    }

    private async Task AssertInvalidCreateAsync(CreateHabitDto dto)
    {
        var service = CreateService();

        await Assert.ThrowsAsync<ValidationException>(
            () => service.CreateHabitAsync(dto));

        _repository.Verify(
            repository => repository.AddAsync(
                It.IsAny<Habit>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private void ConfigureCurrentUser(
        bool isAuthenticated,
        bool emptyUserId)
    {
        _currentUser
            .Setup(user => user.IsAuthenticated)
            .Returns(isAuthenticated);

        _currentUser
            .Setup(user => user.UserId)
            .Returns(emptyUserId ? Guid.Empty : UserId);
    }

    private void VerifyRepositoryNotCalled()
    {
        _repository.Verify(
            repository => repository.GetByIdAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _repository.Verify(
            repository => repository.GetAllByUserIdAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _repository.Verify(
            repository => repository.AddAsync(
                It.IsAny<Habit>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _repository.Verify(
            repository => repository.UpdateAsync(
                It.IsAny<Habit>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _repository.Verify(
            repository => repository.GetLogByDateAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<DateOnly>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _repository.Verify(
            repository => repository.GetLogsByHabitIdAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _repository.Verify(
            repository => repository.GetCompletionDatesAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _repository.Verify(
            repository => repository.TryAddLogAsync(
                It.IsAny<HabitLog>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static CreateHabitDto CreateHabitDto()
    {
        return new CreateHabitDto
        {
            Name = "Read"
        };
    }

    private static Habit CreateHabit(
        string name,
        bool isActive)
    {
        return new Habit
        {
            Id = Guid.NewGuid(),
            UserId = UserId,
            Name = name,
            IsActive = isActive
        };
    }

    private void SetupHabitLookup(Habit habit)
    {
        _repository
            .Setup(repository => repository.GetByIdAsync(
                UserId,
                habit.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(habit);
    }

    private void SetupDate(
        DateOnly currentDate,
        DateTimeOffset? utcNow = null)
    {
        _dateTimeProvider
            .Setup(provider => provider.UtcNow)
            .Returns(utcNow ?? new DateTimeOffset(
                currentDate.ToDateTime(new TimeOnly(12, 0)),
                TimeSpan.Zero));

        _dateTimeProvider
            .Setup(provider => provider.GetCurrentDate("UTC"))
            .Returns(currentDate);
    }
}
