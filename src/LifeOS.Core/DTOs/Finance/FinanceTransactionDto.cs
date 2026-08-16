using LifeOS.Core.Enums.Finance;

namespace LifeOS.Core.DTOs.Finance;

public sealed class FinanceTransactionDto
{
    public Guid Id { get; init; }

    public DateOnly TransactionDate { get; init; }

    public FinanceTransactionType Type { get; init; }

    public decimal Amount { get; init; }

    public Guid CategoryId { get; init; }

    public string CategoryName { get; init; } = string.Empty;

    public string? Description { get; init; }
}
