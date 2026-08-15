using LifeOS.Infrastructure.Extensions;

namespace LifeOS.Tests.Infrastructure;

public sealed class ReminderJobRegistrationTests
{
    [Fact]
    public void UsesStableIdAndEveryMinuteSchedule()
    {
        Assert.Equal("process-due-reminders", ReminderJobRegistration.JobId);
        Assert.Equal("* * * * *", Hangfire.Cron.Minutely());
    }
}
