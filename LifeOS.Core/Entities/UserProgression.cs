using LifeOS.Core.Enums;

namespace LifeOS.Core.Entities
{
    public class UserProgression
    {
        public string UserId { get; set; } = string.Empty;
        public long TotalLifetimeXP { get; set; } = 0;
        public int CurrentLevel { get; set; } = 1;
        public Echelon CurrentEchelon { get; set; } = Echelon.Iron;
        public int DailyQuestXPToday { get; set; } = 0;
        public DateOnly DailyQuestXPDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);

        public ApplicationUser? User { get; set; }
    }
}