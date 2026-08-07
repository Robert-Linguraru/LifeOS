using LifeOS.Core.Entities;

namespace LifeOS.Core.Abstractions.Tasks;

public interface ITaskRepository
{
    Task<TaskItem?> GetByIdAsync(
        Guid userId,
        Guid taskId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TaskItem>> GetAllByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        TaskItem task,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        TaskItem task,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        TaskItem task,
        CancellationToken cancellationToken = default);
}