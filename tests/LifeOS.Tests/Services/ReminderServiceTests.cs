using LifeOS.Core.Abstractions;
using LifeOS.Core.Abstractions.Habits;
using LifeOS.Core.Abstractions.Reminders;
using LifeOS.Core.Abstractions.Tasks;
using LifeOS.Core.Constants;
using LifeOS.Core.DTOs.Reminders;
using LifeOS.Core.Entities;
using LifeOS.Core.Enums.Reminders;
using LifeOS.Core.Enums.Tasks;
using LifeOS.Core.Exceptions;
using LifeOS.Core.Services;
using LifeOS.Core.Time;
using LifeOS.Infrastructure.Services;
using Moq;

namespace LifeOS.Tests.Services;

public sealed class ReminderServiceTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly DateTimeOffset Now =
        new(2026, 8, 20, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset ScheduledUtc = Now.AddHours(2);

    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly Mock<IReminderRepository> _repository = new();
    private readonly Mock<IUserSettingsRepository> _settings = new();
    private readonly Mock<ITaskRepository> _tasks = new();
    private readonly Mock<IHabitRepository> _habits = new();
    private readonly Mock<IDateTimeProvider> _dateTime = new();

    private ReminderService CreateService(bool confirmed = true)
    {
        _currentUser.Setup(user => user.UserId).Returns(UserId);
        _currentUser.Setup(user => user.IsAuthenticated).Returns(true);
        _dateTime.Setup(provider => provider.UtcNow).Returns(Now);
        _dateTime.Setup(provider => provider.IsValidTimeZone("UTC")).Returns(true);
        _dateTime.Setup(provider => provider.ConvertLocalToUtc(
                It.IsAny<DateOnly>(),
                It.IsAny<TimeOnly>(),
                "UTC"))
            .Returns(LocalTimeConversionResult.Success(ScheduledUtc));
        _settings.Setup(repository => repository.GetByUserIdAsync(
                UserId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(confirmed
                ? new UserSettings
                {
                    UserId = UserId,
                    TimeZoneConfiguredAtUtc = Now.AddDays(-1)
                }
                : new UserSettings { UserId = UserId });

        return new ReminderService(
            _currentUser.Object,
            _repository.Object,
            _settings.Object,
            _tasks.Object,
            _habits.Object,
            _dateTime.Object);
    }

    [Fact]
    public async Task CreateAsync_GeneratesCanonicalPendingReminder()
    {
        Reminder? saved = null;
        _repository.Setup(repository => repository.AddAsync(
                It.IsAny<Reminder>(),
                It.IsAny<CancellationToken>()))
            .Callback<Reminder, CancellationToken>((reminder, _) => saved = reminder)
            .Returns(Task.CompletedTask);

        var result = await CreateService().CreateAsync(CreateDto());

        Assert.NotNull(saved);
        Assert.Equal(saved!.Id, result.Id);
        Assert.Equal(ReminderStatus.Pending, saved.Status);
        Assert.Equal(0, saved.Version);
        Assert.Null(saved.FiredAtUtc);
        Assert.Null(saved.NotificationId);
        Assert.Equal($"ReminderFired:{saved.Id:N}", saved.IdempotencyKey);
        Assert.Equal("Title", saved.Title);
        Assert.Null(saved.Message);
    }

    [Fact]
    public async Task CreateAsync_RequiresConfirmedTimeZone()
    {
        await Assert.ThrowsAsync<ValidationException>(
            () => CreateService(false).CreateAsync(CreateDto()));

        _repository.Verify(repository => repository.AddAsync(
            It.IsAny<Reminder>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_RejectsInvalidScheduleAndPrecision()
    {
        var dto = new CreateReminderDto
        {
            SourceType = ReminderSourceType.Custom,
            Title = "Title",
            ScheduledLocalDate = new DateOnly(2026, 8, 20),
            ScheduledLocalTime = new TimeOnly(14, 30, 1),
            TimeZoneId = "UTC"
        };

        await Assert.ThrowsAsync<ValidationException>(
            () => CreateService().CreateAsync(dto));
    }

    [Theory]
    [InlineData(LocalTimeConversionFailure.InvalidLocalTime)]
    [InlineData(LocalTimeConversionFailure.AmbiguousLocalTime)]
    public async Task CreateAsync_RejectsLocalTimeConversionFailures(
        LocalTimeConversionFailure failure)
    {
        var service = CreateService();
        _dateTime.Setup(provider => provider.ConvertLocalToUtc(
                It.IsAny<DateOnly>(),
                It.IsAny<TimeOnly>(),
                "UTC"))
            .Returns(LocalTimeConversionResult.Failed(failure));

        await Assert.ThrowsAsync<ValidationException>(
            () => service.CreateAsync(CreateDto()));
    }

    [Fact]
    public async Task CreateAsync_RejectsPastScheduleAndOversizedMessage()
    {
        var service = CreateService();
        _dateTime.Setup(provider => provider.ConvertLocalToUtc(
                It.IsAny<DateOnly>(),
                It.IsAny<TimeOnly>(),
                "UTC"))
            .Returns(LocalTimeConversionResult.Success(Now));

        var past = CreateDto();
        await Assert.ThrowsAsync<ValidationException>(
            () => service.CreateAsync(past));

        var oversized = new CreateReminderDto
        {
            SourceType = ReminderSourceType.Custom,
            Title = "Title",
            Message = new string('x', ReminderConstants.MessageMaxLength + 1),
            ScheduledLocalDate = new DateOnly(2026, 8, 20),
            ScheduledLocalTime = new TimeOnly(14, 30),
            TimeZoneId = "UTC"
        };
        await Assert.ThrowsAsync<ValidationException>(
            () => service.CreateAsync(oversized));
    }

    [Fact]
    public async Task CreateAsync_RejectsInvalidTimeZone()
    {
        _dateTime.Setup(provider => provider.IsValidTimeZone("Invalid"))
            .Returns(false);

        var dto = CreateDto();
        dto = new CreateReminderDto
        {
            SourceType = ReminderSourceType.Custom,
            Title = dto.Title,
            ScheduledLocalDate = dto.ScheduledLocalDate,
            ScheduledLocalTime = dto.ScheduledLocalTime,
            TimeZoneId = "Invalid"
        };

        await Assert.ThrowsAsync<ValidationException>(
            () => CreateService().CreateAsync(dto));
    }

    [Fact]
    public async Task CreateAsync_ValidatesAndSnapshotsTask()
    {
        var task = new TaskItem
        {
            UserId = UserId,
            Title = "Current task",
            Status = TaskItemStatus.Active
        };
        _tasks.Setup(repository => repository.GetByIdAsync(
                UserId,
                task.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);
        Reminder? saved = null;
        _repository.Setup(repository => repository.AddAsync(
                It.IsAny<Reminder>(), It.IsAny<CancellationToken>()))
            .Callback<Reminder, CancellationToken>((reminder, _) => saved = reminder)
            .Returns(Task.CompletedTask);

        var dto = new CreateReminderDto
        {
            SourceType = ReminderSourceType.Task,
            SourceId = task.Id,
            Title = "Title",
            ScheduledLocalDate = new DateOnly(2026, 8, 20),
            ScheduledLocalTime = new TimeOnly(14, 30),
            TimeZoneId = "UTC"
        };

        await CreateService().CreateAsync(dto);

        Assert.Equal(task.Id, saved!.SourceId);
        Assert.Equal("Current task", saved.SourceTitle);
    }

    [Fact]
    public async Task CreateAsync_RejectsMissingOrForeignTask()
    {
        var taskId = Guid.NewGuid();
        _tasks.Setup(repository => repository.GetByIdAsync(
                UserId,
                taskId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((TaskItem?)null);
        var dto = new CreateReminderDto
        {
            SourceType = ReminderSourceType.Task,
            SourceId = taskId,
            Title = "Title",
            ScheduledLocalDate = new DateOnly(2026, 8, 20),
            ScheduledLocalTime = new TimeOnly(14, 30),
            TimeZoneId = "UTC"
        };

        await Assert.ThrowsAsync<ResourceNotFoundException>(
            () => CreateService().CreateAsync(dto));
    }

    [Fact]
    public async Task GetPendingAsync_DoesNotRequireTimezoneConfirmation()
    {
        var reminder = CreateReminder();
        _repository.Setup(repository => repository.GetPendingAsync(
                UserId,
                ReminderConstants.DefaultListLimit,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([reminder]);

        var result = await CreateService(false).GetPendingAsync();

        Assert.Single(result);
        _settings.Verify(repository => repository.GetByUserIdAsync(
            It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_ForwardsExpectedVersionAndRefreshesSnapshot()
    {
        var existing = CreateReminder();
        var updated = CreateReminder();
        updated.Id = existing.Id;
        updated.Version = 1;
        updated.SourceTitle = "Updated title";
        _repository.SetupSequence(repository => repository.GetByIdAsync(
                UserId,
                existing.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing)
            .ReturnsAsync(updated);
        _repository.Setup(repository => repository.UpdatePendingAsync(
                UserId,
                It.IsAny<Reminder>(),
                0,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await CreateService().UpdateAsync(
            existing.Id,
            new UpdateReminderDto
            {
                SourceType = ReminderSourceType.Custom,
                Title = "Updated",
                ScheduledLocalDate = new DateOnly(2026, 8, 20),
                ScheduledLocalTime = new TimeOnly(14, 30),
                TimeZoneId = "UTC",
                ExpectedVersion = 0
            });

        Assert.Equal(ReminderMutationStatus.Updated, result.Status);
        _repository.Verify(repository => repository.UpdatePendingAsync(
            UserId,
            It.Is<Reminder>(reminder =>
                reminder.Id == existing.Id &&
                reminder.SourceId == null &&
                reminder.SourceTitle == null),
            0,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CancelAsync_ReturnsAlreadyCancelledWithoutTimezoneGate()
    {
        var reminder = CreateReminder();
        reminder.Status = ReminderStatus.Cancelled;
        reminder.Version = 4;
        _repository.Setup(repository => repository.CancelPendingAsync(
                UserId, reminder.Id, 0, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _repository.Setup(repository => repository.GetByIdAsync(
                UserId, reminder.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reminder);

        var result = await CreateService(false).CancelAsync(reminder.Id, 0);

        Assert.Equal(ReminderMutationStatus.AlreadyCancelled, result.Status);
    }

    [Fact]
    public async Task UnauthenticatedUser_IsRejected()
    {
        var service = CreateService();
        _currentUser.Setup(user => user.IsAuthenticated).Returns(false);

        await Assert.ThrowsAsync<CurrentUserUnavailableException>(
            () => service.GetPendingAsync());
    }

    private static CreateReminderDto CreateDto()
    {
        return new CreateReminderDto
        {
            SourceType = ReminderSourceType.Custom,
            Title = "  Title  ",
            Message = "   ",
            ScheduledLocalDate = new DateOnly(2026, 8, 20),
            ScheduledLocalTime = new TimeOnly(14, 30),
            TimeZoneId = " UTC "
        };
    }

    private static Reminder CreateReminder()
    {
        return new Reminder
        {
            UserId = UserId,
            Title = "Reminder",
            ScheduledLocalDate = new DateOnly(2026, 8, 20),
            ScheduledLocalTime = new TimeOnly(14, 30),
            TimeZoneId = "UTC",
            ScheduledForUtc = ScheduledUtc,
            IdempotencyKey = $"ReminderFired:{Guid.NewGuid():N}"
        };
    }
}
