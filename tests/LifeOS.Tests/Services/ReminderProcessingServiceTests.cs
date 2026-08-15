using LifeOS.Core.Abstractions;
using LifeOS.Core.Abstractions.Reminders;
using LifeOS.Core.Services;
using LifeOS.Infrastructure.Exceptions;
using LifeOS.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace LifeOS.Tests.Services;

public sealed class ReminderProcessingServiceTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 8, 20, 12, 0, 0, TimeSpan.Zero);

    private readonly Mock<IReminderRepository> _repository = new();
    private readonly Mock<IDateTimeProvider> _dateTime = new();

    private ReminderProcessingService CreateService()
    {
        _dateTime.Setup(provider => provider.UtcNow).Returns(Now);
        return new ReminderProcessingService(
            _repository.Object,
            _dateTime.Object,
            NullLogger<ReminderProcessingService>.Instance);
    }

    [Fact]
    public async Task EmptyBatch_ReturnsZeroCounts()
    {
        _repository.Setup(repository => repository.GetDueCandidatesAsync(
                Now, 100, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var result = await CreateService().ProcessDueRemindersAsync(100);

        Assert.Equal(0, result.Attempted);
        Assert.Equal(0, result.Fired);
        Assert.Equal(0, result.Skipped);
        Assert.Equal(0, result.Failed);
        _repository.Verify(repository => repository.CommitFireAsync(
            It.IsAny<ReminderFireCommitRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OneFiredCandidate_BuildsDeterministicDraftAndCountsFired()
    {
        var candidate = CreateCandidate();
        _repository.Setup(repository => repository.GetDueCandidatesAsync(
                Now, 100, It.IsAny<CancellationToken>()))
            .ReturnsAsync([candidate]);
        _repository.Setup(repository => repository.CommitFireAsync(
                It.IsAny<ReminderFireCommitRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ReminderFireCommitResult
            {
                Status = ReminderFireCommitStatus.Fired,
                NotificationId = candidate.ReminderId
            });

        var result = await CreateService().ProcessDueRemindersAsync(100);

        Assert.Equal(1, result.Attempted);
        Assert.Equal(1, result.Fired);
        _repository.Verify(repository => repository.CommitFireAsync(
            It.Is<ReminderFireCommitRequest>(request =>
                request.ReminderId == candidate.ReminderId &&
                request.UserId == candidate.UserId &&
                request.ExpectedVersion == candidate.Version &&
                request.DueCutoffUtc == Now &&
                request.FiredAtUtc == Now &&
                request.Notification.NotificationId == candidate.ReminderId &&
                request.Notification.UserId == candidate.UserId &&
                request.Notification.SourceId == candidate.ReminderId &&
                request.Notification.IdempotencyKey == $"ReminderFired:{candidate.ReminderId:N}"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(ReminderFireCommitStatus.AlreadyFired)]
    [InlineData(ReminderFireCommitStatus.Cancelled)]
    [InlineData(ReminderFireCommitStatus.NotDue)]
    [InlineData(ReminderFireCommitStatus.Missing)]
    [InlineData(ReminderFireCommitStatus.ConcurrencyLost)]
    public async Task NormalSkipOutcomes_AreCountedAsSkipped(
        ReminderFireCommitStatus status)
    {
        var candidate = CreateCandidate();
        _repository.Setup(repository => repository.GetDueCandidatesAsync(
                Now, 100, It.IsAny<CancellationToken>()))
            .ReturnsAsync([candidate]);
        _repository.Setup(repository => repository.CommitFireAsync(
                It.IsAny<ReminderFireCommitRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ReminderFireCommitResult { Status = status });

        var result = await CreateService().ProcessDueRemindersAsync(100);

        Assert.Equal(1, result.Attempted);
        Assert.Equal(1, result.Skipped);
        Assert.Equal(0, result.Failed);
    }

    [Fact]
    public async Task UnexpectedFailure_DoesNotPreventLaterCandidatesAndThrowsAggregateFailure()
    {
        var first = CreateCandidate();
        var second = CreateCandidate();
        _repository.Setup(repository => repository.GetDueCandidatesAsync(
                Now, 100, It.IsAny<CancellationToken>()))
            .ReturnsAsync([first, second]);
        _repository.SetupSequence(repository => repository.CommitFireAsync(
                It.IsAny<ReminderFireCommitRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("test failure"))
            .ReturnsAsync(new ReminderFireCommitResult
            {
                Status = ReminderFireCommitStatus.Fired,
                NotificationId = second.ReminderId
            });

        var exception = await Assert.ThrowsAsync<ReminderProcessingBatchException>(
            () => CreateService().ProcessDueRemindersAsync(100));

        Assert.Equal(2, exception.Result.Attempted);
        Assert.Equal(1, exception.Result.Fired);
        Assert.Equal(0, exception.Result.Skipped);
        Assert.Equal(1, exception.Result.Failed);
        _repository.Verify(repository => repository.CommitFireAsync(
            It.IsAny<ReminderFireCommitRequest>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task InvalidBatchSize_IsRejected()
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => CreateService().ProcessDueRemindersAsync(0));
    }

    private static ReminderDueCandidate CreateCandidate()
    {
        return new ReminderDueCandidate(
            Guid.NewGuid(),
            Guid.NewGuid(),
            3,
            Now.AddMinutes(-1));
    }
}
