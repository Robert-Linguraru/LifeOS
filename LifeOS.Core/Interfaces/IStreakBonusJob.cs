namespace LifeOS.Core.Interfaces;

public interface IStreakBonusJob
{
    Task ExecuteAsync(CancellationToken cancellationToken = default);
}