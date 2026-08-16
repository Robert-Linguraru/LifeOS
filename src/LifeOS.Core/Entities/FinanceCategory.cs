using LifeOS.Core.Constants;
using LifeOS.Core.Enums.Finance;

namespace LifeOS.Core.Entities;

public sealed class FinanceCategory : BaseEntity
{
    private FinanceCategory()
    {
    }

    public FinanceCategory(
        Guid id,
        string name,
        FinanceCategoryType type,
        int sortOrder)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("A finance category id is required.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("A finance category name is required.", nameof(name));
        }

        if (name.Length > FinanceConstants.CategoryNameMaxLength)
        {
            throw new ArgumentException(
                "The finance category name is too long.",
                nameof(name));
        }

        if (!Enum.IsDefined(type))
        {
            throw new ArgumentOutOfRangeException(nameof(type));
        }

        if (sortOrder < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sortOrder));
        }

        Id = id;
        Name = name;
        Type = type;
        SortOrder = sortOrder;
        IsActive = true;
    }

    public string Name { get; private set; } = string.Empty;

    public FinanceCategoryType Type { get; private set; }

    public int SortOrder { get; private set; }

    public bool IsActive { get; private set; }

    public bool IsCompatibleWith(FinanceTransactionType transactionType)
    {
        return Type == FinanceCategoryType.Both ||
            (Type == FinanceCategoryType.Income &&
                transactionType == FinanceTransactionType.Income) ||
            (Type == FinanceCategoryType.Expense &&
                transactionType == FinanceTransactionType.Expense);
    }
}
