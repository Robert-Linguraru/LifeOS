using LifeOS.Core.Abstractions.Finance;
using LifeOS.Core.Entities;
using LifeOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LifeOS.Infrastructure.Repositories;

public sealed class FinanceRepository : IFinanceRepository
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public FinanceRepository(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<IReadOnlyList<FinanceCategory>> GetCategoriesAsync(
        CancellationToken cancellationToken = default)
    {
        await using var context =
            await _contextFactory.CreateDbContextAsync(cancellationToken);

        return await context.FinanceCategories
            .AsNoTracking()
            .Where(category => category.IsActive)
            .OrderBy(category => category.SortOrder)
            .ThenBy(category => category.Name)
            .ThenBy(category => category.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<FinanceCategory?> GetCategoryByIdAsync(
        Guid categoryId,
        CancellationToken cancellationToken = default)
    {
        await using var context =
            await _contextFactory.CreateDbContextAsync(cancellationToken);

        return await context.FinanceCategories
            .AsNoTracking()
            .SingleOrDefaultAsync(
                category => category.Id == categoryId && category.IsActive,
                cancellationToken);
    }

    public async Task<FinanceTransaction?> GetTransactionByIdAsync(
        Guid userId,
        Guid transactionId,
        CancellationToken cancellationToken = default)
    {
        await using var context =
            await _contextFactory.CreateDbContextAsync(cancellationToken);

        return await context.FinanceTransactions
            .AsNoTracking()
            .SingleOrDefaultAsync(
                transaction =>
                    transaction.Id == transactionId &&
                    transaction.UserId == userId,
                cancellationToken);
    }

    public async Task<IReadOnlyList<FinanceTransaction>> GetTransactionsByMonthAsync(
        Guid userId,
        DateOnly monthStart,
        DateOnly monthEndExclusive,
        CancellationToken cancellationToken = default)
    {
        await using var context =
            await _contextFactory.CreateDbContextAsync(cancellationToken);

        return await context.FinanceTransactions
            .AsNoTracking()
            .Where(transaction =>
                transaction.UserId == userId &&
                transaction.TransactionDate >= monthStart &&
                transaction.TransactionDate < monthEndExclusive)
            .OrderByDescending(transaction => transaction.TransactionDate)
            .ThenByDescending(transaction => transaction.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<FinanceTransaction>> GetTransactionsByYearAsync(
        Guid userId,
        DateOnly yearStart,
        DateOnly yearEndExclusive,
        CancellationToken cancellationToken = default)
    {
        await using var context =
            await _contextFactory.CreateDbContextAsync(cancellationToken);

        return await context.FinanceTransactions
            .AsNoTracking()
            .Where(transaction =>
                transaction.UserId == userId &&
                transaction.TransactionDate >= yearStart &&
                transaction.TransactionDate < yearEndExclusive)
            .OrderByDescending(transaction => transaction.TransactionDate)
            .ThenByDescending(transaction => transaction.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task AddTransactionAsync(
        FinanceTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        await using var context =
            await _contextFactory.CreateDbContextAsync(cancellationToken);

        context.FinanceTransactions.Add(transaction);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateTransactionAsync(
        FinanceTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        await using var context =
            await _contextFactory.CreateDbContextAsync(cancellationToken);

        var existing = await context.FinanceTransactions
            .SingleOrDefaultAsync(
                item =>
                    item.Id == transaction.Id &&
                    item.UserId == transaction.UserId,
                cancellationToken);

        if (existing is null)
        {
            return;
        }

        context.Entry(existing).CurrentValues.SetValues(transaction);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteTransactionAsync(
        Guid userId,
        Guid transactionId,
        CancellationToken cancellationToken = default)
    {
        await using var context =
            await _contextFactory.CreateDbContextAsync(cancellationToken);

        var transaction = await context.FinanceTransactions
            .SingleOrDefaultAsync(
                item =>
                    item.Id == transactionId &&
                    item.UserId == userId,
                cancellationToken);

        if (transaction is null)
        {
            return;
        }

        context.FinanceTransactions.Remove(transaction);
        await context.SaveChangesAsync(cancellationToken);
    }
}
