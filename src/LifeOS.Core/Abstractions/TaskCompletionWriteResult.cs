using LifeOS.Core.Entities;

namespace LifeOS.Core.Abstractions;

public enum TaskCompletionWriteStatus
{
    NewlyCompleted = 0,
    AlreadyCompleted = 1,
    NotFound = 2,
    Archived = 3
}

public sealed class TaskCompletionWriteResult
{
    public TaskCompletionWriteStatus Status { get; init; }

    public TaskItem? Task { get; init; }
}
