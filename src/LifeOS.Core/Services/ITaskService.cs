using LifeOS.Core.DTOs.Tasks;

namespace LifeOS.Core.Services;

public interface ITaskService
{
    Task<TaskDetailsDto> CreateTaskAsync(
        CreateTaskDto dto,
        CancellationToken cancellationToken = default);

    Task<TaskDetailsDto> UpdateTaskAsync(
        Guid taskId,
        UpdateTaskDto dto,
        CancellationToken cancellationToken = default);

    Task<TaskDetailsDto> GetTaskByIdAsync(
        Guid taskId,
        CancellationToken cancellationToken = default);

    Task<TaskListDto> GetTaskListAsync(
        CancellationToken cancellationToken = default);

    Task<TaskDetailsDto> CompleteTaskAsync(
        Guid taskId,
        CancellationToken cancellationToken = default);

    Task<TaskDetailsDto> ArchiveTaskAsync(
        Guid taskId,
        CancellationToken cancellationToken = default);

    Task DeleteTaskAsync(
        Guid taskId,
        CancellationToken cancellationToken = default);
}