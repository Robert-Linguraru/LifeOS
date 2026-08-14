using LifeOS.Core.DTOs.Xp;

namespace LifeOS.Core.DTOs.Tasks;

public sealed class TaskCompletionResultDto
{
    public TaskDetailsDto Task { get; init; } = new();

    public bool WasNewlyCompleted { get; init; }

    public XpAwardResultDto? XpAward { get; init; }

    public bool XpAwardFailed { get; init; }
}
