using LifeOS.Core.DTOs.Xp;

namespace LifeOS.Core.Services;

public interface IXpService
{
    Task<XpAwardResultDto> AwardQuestXpAsync(
        AwardQuestXpDto dto,
        CancellationToken cancellationToken = default);

    Task<UserProgressionDto> GetProgressionAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<XpTransactionDto>> GetXpHistoryAsync(
        CancellationToken cancellationToken = default);
}
