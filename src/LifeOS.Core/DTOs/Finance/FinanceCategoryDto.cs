using LifeOS.Core.Enums.Finance;

namespace LifeOS.Core.DTOs.Finance;

public sealed class FinanceCategoryDto
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public FinanceCategoryType Type { get; init; }
}
