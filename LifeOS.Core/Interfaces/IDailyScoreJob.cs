namespace LifeOS.Core.Interfaces;

public interface IDailyScoreJob
{
    Task ExecuteAsync(CancellationToken cancellationToken = default);
}