namespace LifeOS.Core.Entities
{
    public class DailyScore
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string UserId { get; set; } = string.Empty;

        public DateOnly Date { get; set; }
        public int HabitScore { get; set; }    // 0-30
        public int TaskScore { get; set; }     // 0-20
        public int SleepScore { get; set; }    // 0-15
        public int WorkoutScore { get; set; }  // 0-15
        public int FinanceScore { get; set; }  // 0-10
        public int NutritionScore { get; set; }// 0-10
        public int TotalScore { get; set; }    // 0-100
        public int XPAwarded { get; set; }

        public ApplicationUser? User { get; set; }
    }
}