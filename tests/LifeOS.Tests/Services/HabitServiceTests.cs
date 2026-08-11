using LifeOS.Core.Abstractions;
using LifeOS.Core.Abstractions.Habits;
using LifeOS.Core.DTOs.Habits;
using LifeOS.Core.Entities;
using LifeOS.Core.Enums;
using LifeOS.Core.Enums.Habits;
using LifeOS.Core.Exceptions;
using LifeOS.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace LifeOS.Tests.Services;

public sealed class HabitServiceTests
{
    private static readonly Guid UserId = Guid.NewGuid();

    private readonly Mock<IHabitRepository> _repository = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly Mock<ILogger<HabitService>> _logger = new();

    private HabitService CreateService()
    {
        _currentUser
            .Setup(user => user.UserId)
            .Returns(UserId);

        _currentUser
            .Setup(user => user.IsAuthenticated)
            .Returns(true);

        return new HabitService(
            _repository.Object,
            _currentUser.Object,
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
}
