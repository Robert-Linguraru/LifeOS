namespace LifeOS.Core.Progression;

public static class XpIdempotencyKeyFactory
{
    public static string ForTask(Guid taskId)
    {
        ValidateId(taskId, nameof(taskId));

        return $"TaskComplete:{taskId:D}";
    }

    public static string ForHabit(Guid habitId, DateOnly completionDate)
    {
        ValidateId(habitId, nameof(habitId));

        return $"HabitComplete:{habitId:D}:{completionDate:yyyy-MM-dd}";
    }

    private static void ValidateId(Guid id, string parameterName)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException(
                "The source ID cannot be empty.",
                parameterName);
        }
    }
}
