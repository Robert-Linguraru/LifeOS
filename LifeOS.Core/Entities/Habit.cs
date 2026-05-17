using LifeOS.Core.Enums;

namespace LifeOS.Core.Entities
{
    public class Habit
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string UserId { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }

        public HabitFrequency Frequency { get; set; } = HabitFrequency.Daily;
        public string? SelectedDays { get; set; } // e.g. "Mon,Wed,Fri" for SelectedDays frequency

        // Quantitative habits
        public bool IsQuantitative { get; set; } = false;
        public decimal? TargetValue { get; set; }
        public string? Unit { get; set; } // e.g. "liters", "pages", "minutes"

        // Gamification
        public EstimatedTime EstimatedTime { get; set; } = EstimatedTime.Under15m;
        public FrictionLevel FrictionLevel { get; set; } = FrictionLevel.Low;
        public QuestCategory QuestCategory { get; set; } = QuestCategory.Routines;
        public int? CalculatedXP { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ApplicationUser? User { get; set; }
        public ICollection<HabitLog> Logs { get; set; } = new List<HabitLog>();
    }
}