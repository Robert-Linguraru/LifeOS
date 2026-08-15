using Hangfire;
using LifeOS.Core.Abstractions.Reminders;
using LifeOS.Core.Services;
using LifeOS.Infrastructure.Jobs;
using LifeOS.Infrastructure.Options;
using Microsoft.Extensions.Options;
using Moq;

namespace LifeOS.Tests.Infrastructure;

public sealed class DueReminderJobTests
{
    [Fact]
    public async Task ExecuteAsync_ForwardsConfiguredBatchAndCancellation()
    {
        var processing = new Mock<IReminderProcessingService>();
        var cancellation = new CancellationTokenSource().Token;
        processing.Setup(service => service.ProcessDueRemindersAsync(
                42,
                cancellation))
            .ReturnsAsync(new ReminderProcessingResult());
        var job = new DueReminderJob(
            processing.Object,
            Options.Create(new ReminderProcessingOptions { BatchSize = 42 }));

        await job.ExecuteAsync(cancellation);

        processing.Verify(service => service.ProcessDueRemindersAsync(
            42,
            cancellation), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_PropagatesProcessingFailure()
    {
        var processing = new Mock<IReminderProcessingService>();
        processing.Setup(service => service.ProcessDueRemindersAsync(
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("processing failed"));
        var job = new DueReminderJob(
            processing.Object,
            Options.Create(new ReminderProcessingOptions()));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => job.ExecuteAsync());
    }

    [Fact]
    public void ExecuteAsync_HasRetryAndOverlapAttributes()
    {
        var retry = typeof(DueReminderJob).GetCustomAttributes(
            typeof(AutomaticRetryAttribute), false)
            .Cast<AutomaticRetryAttribute>()
            .Single();
        var overlap = typeof(DueReminderJob).GetCustomAttributes(
            typeof(DisableConcurrentExecutionAttribute), false)
            .Cast<DisableConcurrentExecutionAttribute>()
            .Single();

        Assert.Equal(3, retry.Attempts);
        Assert.NotNull(overlap);
        Assert.NotNull(typeof(DueReminderJob).GetMethod(nameof(DueReminderJob.ExecuteAsync)));
    }
}
