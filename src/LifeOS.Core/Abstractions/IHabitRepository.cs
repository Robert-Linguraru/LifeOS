using LifeOS.Core.Entities;

namespace LifeOS.Core.Abstractions.Habits;

public interface IHabitRepository
{
    Task<Habit?> GetByIdAsync(
        Guid userId,
        Guid habitId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Habit>> GetAllByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Habit habit,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        Habit habit,
        CancellationToken cancellationToken = default);

    Task<HabitLog?> GetLogByDateAsync(
        Guid userId,
        Guid habitId,
        DateOnly completionDate,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<HabitLog>> GetLogsByHabitIdAsync(
        Guid userId,
        Guid habitId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DateOnly>> GetCompletionDatesAsync(
        Guid userId,
        Guid habitId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<(Guid HabitId, DateOnly CompletionDate)>>
        GetCompletionDatesByUserIdAsync(
            Guid userId,
            CancellationToken cancellationToken = default);

    Task<bool> TryAddLogAsync(
        HabitLog habitLog,
        CancellationToken cancellationToken = default);
}
