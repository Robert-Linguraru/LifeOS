using Hangfire;
using LifeOS.Infrastructure.Jobs;

namespace LifeOS.Infrastructure.Extensions;

public static class ReminderJobRegistration
{
    public const string JobId = "process-due-reminders";

    public static void RegisterDueReminderJob(
        IRecurringJobManager recurringJobs)
    {
        recurringJobs.AddOrUpdate<DueReminderJob>(
            JobId,
            job => job.ExecuteAsync(CancellationToken.None),
            Cron.Minutely);
    }
}
