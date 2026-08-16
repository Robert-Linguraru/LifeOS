using LifeOS.Core.Enums.Finance;

namespace LifeOS.Core.DTOs.Finance;

public sealed class CreateFinanceTransactionDto
{
    public DateOnly TransactionDate { get; set; }

    public FinanceTransactionType Type { get; set; }

    public decimal Amount { get; set; }

    public Guid CategoryId { get; set; }

    public string? Description { get; set; }
}
