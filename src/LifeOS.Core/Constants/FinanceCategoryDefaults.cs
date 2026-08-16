using LifeOS.Core.Enums.Finance;

namespace LifeOS.Core.Constants;

public static class FinanceCategoryDefaults
{
    public static readonly Guid SalaryId =
        Guid.Parse("6f2bb6e3-2d1a-4c0f-8e84-5a02f3e0a101");

    public static readonly Guid OtherIncomeId =
        Guid.Parse("6f2bb6e3-2d1a-4c0f-8e84-5a02f3e0a102");

    public static readonly Guid HousingRentId =
        Guid.Parse("6f2bb6e3-2d1a-4c0f-8e84-5a02f3e0a201");

    public static readonly Guid FoodId =
        Guid.Parse("6f2bb6e3-2d1a-4c0f-8e84-5a02f3e0a202");

    public static readonly Guid TransportId =
        Guid.Parse("6f2bb6e3-2d1a-4c0f-8e84-5a02f3e0a203");

    public static readonly Guid FitnessGymId =
        Guid.Parse("6f2bb6e3-2d1a-4c0f-8e84-5a02f3e0a204");

    public static readonly Guid TravelId =
        Guid.Parse("6f2bb6e3-2d1a-4c0f-8e84-5a02f3e0a205");

    public static readonly Guid ShoppingId =
        Guid.Parse("6f2bb6e3-2d1a-4c0f-8e84-5a02f3e0a206");

    public static readonly Guid EntertainmentId =
        Guid.Parse("6f2bb6e3-2d1a-4c0f-8e84-5a02f3e0a207");

    public static readonly Guid BillsUtilitiesId =
        Guid.Parse("6f2bb6e3-2d1a-4c0f-8e84-5a02f3e0a208");

    public static readonly Guid HealthId =
        Guid.Parse("6f2bb6e3-2d1a-4c0f-8e84-5a02f3e0a209");

    public static readonly Guid OtherExpenseId =
        Guid.Parse("6f2bb6e3-2d1a-4c0f-8e84-5a02f3e0a210");

    public static IReadOnlyList<FinanceCategoryDefinition> All { get; } =
    [
        new(SalaryId, "Salary", FinanceCategoryType.Income, 1),
        new(OtherIncomeId, "Other Income", FinanceCategoryType.Income, 2),
        new(HousingRentId, "Housing/Rent", FinanceCategoryType.Expense, 101),
        new(FoodId, "Food", FinanceCategoryType.Expense, 102),
        new(TransportId, "Transport", FinanceCategoryType.Expense, 103),
        new(FitnessGymId, "Fitness/Gym", FinanceCategoryType.Expense, 104),
        new(TravelId, "Travel", FinanceCategoryType.Expense, 105),
        new(ShoppingId, "Shopping", FinanceCategoryType.Expense, 106),
        new(EntertainmentId, "Entertainment", FinanceCategoryType.Expense, 107),
        new(BillsUtilitiesId, "Bills/Utilities", FinanceCategoryType.Expense, 108),
        new(HealthId, "Health", FinanceCategoryType.Expense, 109),
        new(OtherExpenseId, "Other", FinanceCategoryType.Expense, 110)
    ];
}

public sealed record FinanceCategoryDefinition(
    Guid Id,
    string Name,
    FinanceCategoryType Type,
    int SortOrder);
