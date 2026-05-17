namespace LifeOS.Core.Entities
{
    public class HabitLog
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid HabitId { get; set; }
        public string UserId { get; set; } = string.Empty;

        public DateOnly Date { get; set; }
        public bool Completed { get; set; } = true;
        public decimal? ActualValue { get; set; } // for quantitative habits
        public int XPAwarded { get; set; } = 0;

        public DateTime LoggedAt { get; set; } = DateTime.UtcNow;

        public Habit? Habit { get; set; }
    }
}