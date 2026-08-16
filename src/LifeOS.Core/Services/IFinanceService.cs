using LifeOS.Core.DTOs.Finance;

namespace LifeOS.Core.Services;

public interface IFinanceService
{
    Task<FinanceTransactionDto> CreateTransactionAsync(
        CreateFinanceTransactionDto dto,
        CancellationToken cancellationToken = default);

    Task<FinanceTransactionDto> GetTransactionAsync(
        Guid transactionId,
        CancellationToken cancellationToken = default);

    Task<FinanceTransactionDto> UpdateTransactionAsync(
        Guid transactionId,
        UpdateFinanceTransactionDto dto,
        CancellationToken cancellationToken = default);

    Task DeleteTransactionAsync(
        Guid transactionId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FinanceCategoryDto>> GetCategoriesAsync(
        CancellationToken cancellationToken = default);

    Task<FinanceSummaryDto> GetMonthlySummaryAsync(
        int year,
        int month,
        CancellationToken cancellationToken = default);

    Task<FinanceYearSummaryDto> GetYearSummaryAsync(
        int year,
        CancellationToken cancellationToken = default);
}
