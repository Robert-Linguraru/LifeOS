using LifeOS.Core.DTOs.Habits;

namespace LifeOS.Core.Services;

public interface IHabitService
{
    Task<HabitDetailsDto> CreateHabitAsync(
        CreateHabitDto dto,
        CancellationToken cancellationToken = default);

    Task<HabitDetailsDto> UpdateHabitAsync(
        Guid habitId,
        UpdateHabitDto dto,
        CancellationToken cancellationToken = default);

    Task<HabitDetailsDto> GetHabitByIdAsync(
        Guid habitId,
        CancellationToken cancellationToken = default);

    Task<HabitDetailsDto> ArchiveHabitAsync(
        Guid habitId,
        CancellationToken cancellationToken = default);
}
