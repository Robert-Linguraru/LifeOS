using LifeOS.Core.Entities;

namespace LifeOS.Core.Abstractions.Finance;

public interface IFinanceRepository
{
    Task<IReadOnlyList<FinanceCategory>> GetCategoriesAsync(
        CancellationToken cancellationToken = default);

    Task<FinanceCategory?> GetCategoryByIdAsync(
        Guid categoryId,
        CancellationToken cancellationToken = default);

    Task<FinanceTransaction?> GetTransactionByIdAsync(
        Guid userId,
        Guid transactionId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FinanceTransaction>> GetTransactionsByMonthAsync(
        Guid userId,
        DateOnly monthStart,
        DateOnly monthEndExclusive,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FinanceTransaction>> GetTransactionsByYearAsync(
        Guid userId,
        DateOnly yearStart,
        DateOnly yearEndExclusive,
        CancellationToken cancellationToken = default);

    Task AddTransactionAsync(
        FinanceTransaction transaction,
        CancellationToken cancellationToken = default);

    Task UpdateTransactionAsync(
        FinanceTransaction transaction,
        CancellationToken cancellationToken = default);

    Task DeleteTransactionAsync(
        Guid userId,
        Guid transactionId,
        CancellationToken cancellationToken = default);
}
