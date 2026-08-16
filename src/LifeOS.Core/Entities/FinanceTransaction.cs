using LifeOS.Core.Constants;
using LifeOS.Core.Enums.Finance;

namespace LifeOS.Core.Entities;

public sealed class FinanceTransaction : UserOwnedEntity
{
    private FinanceTransaction()
    {
    }

    public FinanceTransaction(
        Guid userId,
        DateOnly transactionDate,
        FinanceTransactionType type,
        decimal amount,
        Guid categoryId,
        string? description)
    {
        UserId = userId;
        Update(transactionDate, type, amount, categoryId, description);
    }

    public DateOnly TransactionDate { get; private set; }

    public FinanceTransactionType Type { get; private set; }

    public decimal Amount { get; private set; }

    public Guid CategoryId { get; private set; }

    public string? Description { get; private set; }

    public void Update(
        DateOnly transactionDate,
        FinanceTransactionType type,
        decimal amount,
        Guid categoryId,
        string? description)
    {
        if (UserId == Guid.Empty)
        {
            throw new ArgumentException("A finance transaction user is required.", nameof(UserId));
        }

        if (transactionDate == default)
        {
            throw new ArgumentException("A transaction date is required.", nameof(transactionDate));
        }

        if (!Enum.IsDefined(type))
        {
            throw new ArgumentOutOfRangeException(nameof(type));
        }

        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(amount),
                "A finance transaction amount must be greater than zero.");
        }

        if (categoryId == Guid.Empty)
        {
            throw new ArgumentException("A finance category is required.", nameof(categoryId));
        }

        var normalizedDescription = description?.Trim();
        if (normalizedDescription?.Length > FinanceConstants.DescriptionMaxLength)
        {
            throw new ArgumentException(
                "The finance transaction description is too long.",
                nameof(description));
        }

        TransactionDate = transactionDate;
        Type = type;
        Amount = amount;
        CategoryId = categoryId;
        Description = string.IsNullOrWhiteSpace(normalizedDescription)
            ? null
            : normalizedDescription;
    }
}
