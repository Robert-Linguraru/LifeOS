using LifeOS.Core.Enums;

namespace LifeOS.Core.Entities
{
    public class StreakRecord
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string UserId { get; set; } = string.Empty;

        public Guid SourceId { get; set; } // HabitId or TaskId
        public StreakSourceType SourceType { get; set; }

        public int CurrentStreak { get; set; } = 0;
        public int LongestStreak { get; set; } = 0;
        public DateOnly? LastCompletedDate { get; set; }

        public ApplicationUser? User { get; set; }
    }
}