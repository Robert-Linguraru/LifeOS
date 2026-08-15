using LifeOS.Core.DTOs.Reminders;
using LifeOS.Core.Constants;

namespace LifeOS.Core.Services;

public interface IReminderService
{
    Task<ReminderDetailsDto> CreateAsync(
        CreateReminderDto dto,
        CancellationToken cancellationToken = default);

    Task<ReminderDetailsDto> GetDetailsAsync(
        Guid reminderId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ReminderSummaryDto>> GetPendingAsync(
        CancellationToken cancellationToken = default,
        int limit = ReminderConstants.DefaultListLimit);

    Task<ReminderMutationResultDto> UpdateAsync(
        Guid reminderId,
        UpdateReminderDto dto,
        CancellationToken cancellationToken = default);

    Task<ReminderMutationResultDto> CancelAsync(
        Guid reminderId,
        long expectedVersion,
        CancellationToken cancellationToken = default);
}
