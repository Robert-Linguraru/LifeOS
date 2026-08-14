using LifeOS.Core.Entities;

namespace LifeOS.Core.Abstractions;

public sealed class HabitLogWriteResult
{
    public bool WasInserted { get; init; }

    public HabitLog? Log { get; init; }
}
