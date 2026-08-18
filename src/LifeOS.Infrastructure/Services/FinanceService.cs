using LifeOS.Core.Abstractions;
using LifeOS.Core.Abstractions.Finance;
using LifeOS.Core.DTOs.Finance;
using LifeOS.Core.Entities;
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

    public Task<FinanceSummaryDto> GetMonthlySummaryAsync(
        int year,
        int month,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException(
            "Finance summaries are not implemented yet.");
    }

    public Task<FinanceYearSummaryDto> GetYearSummaryAsync(
        int year,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException(
            "Finance summaries are not implemented yet.");
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
