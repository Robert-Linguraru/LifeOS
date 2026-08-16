using LifeOS.Core.DTOs.Finance;
using LifeOS.Core.Entities;

namespace LifeOS.Core.Mappings;

public static class FinanceMappings
{
    public static FinanceCategoryDto ToDto(this FinanceCategory category)
    {
        return new FinanceCategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            Type = category.Type
        };
    }

    public static FinanceTransactionDto ToDto(
        this FinanceTransaction transaction,
        string categoryName)
    {
        return new FinanceTransactionDto
        {
            Id = transaction.Id,
            TransactionDate = transaction.TransactionDate,
            Type = transaction.Type,
            Amount = transaction.Amount,
            CategoryId = transaction.CategoryId,
            CategoryName = categoryName,
            Description = transaction.Description
        };
    }
}
