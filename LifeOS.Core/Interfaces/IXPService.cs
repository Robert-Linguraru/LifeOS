using LifeOS.Core.Entities;

namespace LifeOS.Core.Interfaces
{
    public interface IXPService
    {
        int CalculateQuestXP(Core.Enums.EstimatedTime time, Core.Enums.FrictionLevel friction);
        Task<int> AwardQuestXPAsync(string userId, Guid sourceEntityId, Core.Enums.EstimatedTime time, Core.Enums.FrictionLevel friction, Core.Enums.XPSource source);
        Task AwardFlatXPAsync(string userId, int amount, Core.Enums.XPSource source, Guid? sourceEntityId = null, string? notes = null);
        Task<UserProgression?> GetProgressionAsync(string userId);
        Task UpdateStreakAsync(string userId, Guid sourceId, Core.Enums.StreakSourceType sourceType);
    }
}