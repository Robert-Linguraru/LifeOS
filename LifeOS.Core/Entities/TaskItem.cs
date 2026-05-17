using LifeOS.Core.Enums;

namespace LifeOS.Core.Entities
{
    public class TaskItem
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string UserId { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;
        public string? Notes { get; set; }

        public TaskPriority Priority { get; set; } = TaskPriority.Medium;
        public TaskCategory Category { get; set; } = TaskCategory.Personal;
        public Enums.TaskStatus Status { get; set; } = Enums.TaskStatus.Pending;

        public DateTime? DueDate { get; set; }
        public bool IsRecurring { get; set; } = false;
        public string? RecurrenceRule { get; set; } // e.g. "daily", "weekly" — expanded in Milestone 3

        // Gamification fields
        public EstimatedTime EstimatedTime { get; set; } = EstimatedTime.Under15m;
        public FrictionLevel FrictionLevel { get; set; } = FrictionLevel.Low;
        public QuestCategory QuestCategory { get; set; } = QuestCategory.Productivity;
        public int? CalculatedXP { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }

        public ApplicationUser? User { get; set; }
    }
}