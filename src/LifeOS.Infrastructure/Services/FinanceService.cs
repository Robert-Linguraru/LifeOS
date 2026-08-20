using LifeOS.Core.Abstractions;
using LifeOS.Core.Abstractions.Finance;
using LifeOS.Core.DTOs.Finance;
using LifeOS.Core.Entities;
using LifeOS.Core.Enums.Finance;
using LifeOS.Core.Exceptions;
using LifeOS.Core.Mappings;
using LifeOS.Core.Services;
using Microsoft.Extensions.Logging;

namespace LifeOS.Infrastructure.Services;

public sealed class FinanceService : IFinanceService
{
    private readonly IFinanceRepository _repository;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<FinanceService> _logger;

    public FinanceService(
        IFinanceRepository repository,
        ICurrentUserService currentUser,
        ILogger<FinanceService> logger)
    {
        _repository = repository;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<IReadOnlyList<FinanceCategoryDto>> GetCategoriesAsync(
        CancellationToken cancellationToken = default)
    {
        var categories = await _repository.GetCategoriesAsync(cancellationToken);
        return categories.Select(category => category.ToDto()).ToList();
    }

    public async Task<FinanceTransactionDto> CreateTransactionAsync(
        CreateFinanceTransactionDto dto,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var userId = GetCurrentUserId();
        var category = await GetActiveCategoryAsync(
            dto.CategoryId,
            cancellationToken);
        ValidateCategoryCompatibility(category, dto.Type);

        var transaction = new FinanceTransaction(
            userId,
            dto.TransactionDate,
            dto.Type,
            dto.Amount,
            category.Id,
            dto.Description);

        await _repository.AddTransactionAsync(transaction, cancellationToken);

        _logger.LogInformation(
            "Created finance transaction {TransactionId} for user {UserId} with category {CategoryId} and type {TransactionType}",
            transaction.Id,
            userId,
            category.Id,
            transaction.Type);

        return transaction.ToDto(category.Name);
    }

    public async Task<FinanceTransactionDto> GetTransactionAsync(
        Guid transactionId,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        var transaction = await _repository.GetTransactionByIdAsync(
            userId,
            transactionId,
            cancellationToken);

        if (transaction is null)
        {
            throw new ResourceNotFoundException(
                "Finance transaction was not found.");
        }

        var category = await GetActiveCategoryAsync(
            transaction.CategoryId,
            cancellationToken);

        return transaction.ToDto(category.Name);
    }

    public async Task<FinanceTransactionDto> UpdateTransactionAsync(
        Guid transactionId,
        UpdateFinanceTransactionDto dto,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var userId = GetCurrentUserId();
        var transaction = await _repository.GetTransactionByIdAsync(
            userId,
            transactionId,
            cancellationToken);

        if (transaction is null)
        {
            throw new ResourceNotFoundException(
                "Finance transaction was not found.");
        }

        var category = await GetActiveCategoryAsync(
            dto.CategoryId,
            cancellationToken);
        ValidateCategoryCompatibility(category, dto.Type);

        transaction.Update(
            dto.TransactionDate,
            dto.Type,
            dto.Amount,
            category.Id,
            dto.Description);

        await _repository.UpdateTransactionAsync(transaction, cancellationToken);

        _logger.LogInformation(
            "Updated finance transaction {TransactionId} for user {UserId} with category {CategoryId} and type {TransactionType}",
            transaction.Id,
            userId,
            category.Id,
            transaction.Type);

        return transaction.ToDto(category.Name);
    }

    public async Task DeleteTransactionAsync(
        Guid transactionId,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        var transaction = await _repository.GetTransactionByIdAsync(
            userId,
            transactionId,
            cancellationToken);

        if (transaction is null)
        {
            throw new ResourceNotFoundException(
                "Finance transaction was not found.");
        }

        await _repository.DeleteTransactionAsync(
            userId,
            transactionId,
            cancellationToken);

        _logger.LogInformation(
            "Deleted finance transaction {TransactionId} for user {UserId}",
            transactionId,
            userId);
    }

    public async Task<FinanceSummaryDto> GetMonthlySummaryAsync(
        int year,
        int month,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        var (monthStart, monthEndExclusive) = GetMonthBounds(year, month);
        var transactions = await _repository.GetTransactionsByMonthAsync(
            userId,
            monthStart,
            monthEndExclusive,
            cancellationToken);

        if (transactions.Count == 0)
        {
            return new FinanceSummaryDto
            {
                Year = year,
                Month = month
            };
        }

        var categories = await GetCategoriesByIdAsync(cancellationToken);
        var totalIncome = transactions
            .Where(transaction => transaction.Type == FinanceTransactionType.Income)
            .Sum(transaction => transaction.Amount);
        var totalExpenses = transactions
            .Where(transaction => transaction.Type == FinanceTransactionType.Expense)
            .Sum(transaction => transaction.Amount);
        var expenseCategories = transactions
            .Where(transaction => transaction.Type == FinanceTransactionType.Expense)
            .GroupBy(transaction => transaction.CategoryId)
            .Select(group =>
            {
                var category = GetCategory(categories, group.Key);
                return new FinanceCategorySummaryDto
                {
                    CategoryId = category.Id,
                    CategoryName = category.Name,
                    TotalExpenses = group.Sum(transaction => transaction.Amount)
                };
            })
            .OrderBy(summary => categories[summary.CategoryId].SortOrder)
            .ThenBy(summary => summary.CategoryName)
            .ThenBy(summary => summary.CategoryId)
            .ToList();
        var transactionDtos = transactions
            .Select(transaction => transaction.ToDto(
                GetCategory(categories, transaction.CategoryId).Name))
            .ToList();

        return new FinanceSummaryDto
        {
            Year = year,
            Month = month,
            TotalIncome = totalIncome,
            TotalExpenses = totalExpenses,
            ExpenseCategories = expenseCategories,
            Transactions = transactionDtos
        };
    }

    public async Task<FinanceYearSummaryDto> GetYearSummaryAsync(
        int year,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        var (yearStart, yearEndExclusive) = GetYearBounds(year);
        var transactions = await _repository.GetTransactionsByYearAsync(
            userId,
            yearStart,
            yearEndExclusive,
            cancellationToken);

        var totalIncome = transactions
            .Where(transaction => transaction.Type == FinanceTransactionType.Income)
            .Sum(transaction => transaction.Amount);
        var totalExpenses = transactions
            .Where(transaction => transaction.Type == FinanceTransactionType.Expense)
            .Sum(transaction => transaction.Amount);

        return new FinanceYearSummaryDto
        {
            Year = year,
            TotalIncome = totalIncome,
            TotalExpenses = totalExpenses
        };
    }

    private async Task<Dictionary<Guid, FinanceCategory>> GetCategoriesByIdAsync(
        CancellationToken cancellationToken)
    {
        var categories = await _repository.GetCategoriesAsync(cancellationToken);
        return categories.ToDictionary(category => category.Id);
    }

    private static FinanceCategory GetCategory(
        IReadOnlyDictionary<Guid, FinanceCategory> categories,
        Guid categoryId)
    {
        return categories.TryGetValue(categoryId, out var category)
            ? category
            : throw new ResourceNotFoundException(
                "Finance category was not found.");
    }

    private static (DateOnly Start, DateOnly EndExclusive) GetMonthBounds(
        int year,
        int month)
    {
        ValidateYear(year);

        if (month is < 1 or > 12)
        {
            throw new ArgumentOutOfRangeException(nameof(month));
        }

        var start = new DateOnly(year, month, 1);
        return (start, start.AddMonths(1));
    }

    private static (DateOnly Start, DateOnly EndExclusive) GetYearBounds(int year)
    {
        ValidateYear(year);

        return (
            new DateOnly(year, 1, 1),
            new DateOnly(year + 1, 1, 1));
    }

    private static void ValidateYear(int year)
    {
        if (year is < 1 or >= 9999)
        {
            throw new ArgumentOutOfRangeException(nameof(year));
        }
    }

    private async Task<FinanceCategory> GetActiveCategoryAsync(
        Guid categoryId,
        CancellationToken cancellationToken)
    {
        var category = await _repository.GetCategoryByIdAsync(
            categoryId,
            cancellationToken);

        return category ?? throw new ResourceNotFoundException(
            "Finance category was not found.");
    }

    private static void ValidateCategoryCompatibility(
        FinanceCategory category,
        LifeOS.Core.Enums.Finance.FinanceTransactionType transactionType)
    {
        if (!category.IsCompatibleWith(transactionType))
        {
            throw new ValidationException(
                "The finance category is not compatible with the transaction type.");
        }
    }

    private Guid GetCurrentUserId()
    {
        if (!_currentUser.IsAuthenticated ||
            _currentUser.UserId == Guid.Empty)
        {
            throw new CurrentUserUnavailableException();
        }

        return _currentUser.UserId;
    }
}
