namespace LifeOS.Core.Entities;

public sealed class HabitLog : UserOwnedEntity
{
    public Guid HabitId { get; set; }

    public DateOnly CompletionDate { get; set; }

    public DateTimeOffset CompletedAtUtc { get; set; }
}
