using LifeOS.Core.Abstractions;
using LifeOS.Core.Abstractions.Finance;
using LifeOS.Core.Constants;
using LifeOS.Core.DTOs.Finance;
using LifeOS.Core.Entities;
using LifeOS.Core.Enums.Finance;
using LifeOS.Core.Exceptions;
using LifeOS.Core.Services;
using LifeOS.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace LifeOS.Tests.Services;

public sealed class FinanceServiceTests
{
    private static readonly Guid UserId = Guid.NewGuid();

    private readonly Mock<IFinanceRepository> _repository = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly Mock<ILogger<FinanceService>> _logger = new();

    private FinanceService CreateService(bool authenticated = true)
    {
        _currentUser.Setup(item => item.IsAuthenticated).Returns(authenticated);
        _currentUser.Setup(item => item.UserId).Returns(UserId);
        return new FinanceService(
            _repository.Object,
            _currentUser.Object,
            _logger.Object);
    }

    [Fact]
    public async Task GetCategoriesAsync_ReturnsMappedActiveCategories()
    {
        var categories = new[]
        {
            Category(FinanceCategoryDefaults.SalaryId, "Salary", FinanceCategoryType.Income, 1),
            Category(FinanceCategoryDefaults.FoodId, "Food", FinanceCategoryType.Expense, 102)
        };
        _repository.Setup(item => item.GetCategoriesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(categories);

        var result = await CreateService().GetCategoriesAsync();

        Assert.Equal(categories.Select(item => item.Id), result.Select(item => item.Id));
        Assert.Equal("Salary", result[0].Name);
    }

    [Fact]
    public async Task CreateTransactionAsync_UsesCurrentUserAndValidatesCategory()
    {
        var category = Category(
            FinanceCategoryDefaults.SalaryId,
            "Salary",
            FinanceCategoryType.Income,
            1);
        _repository.Setup(item => item.GetCategoryByIdAsync(
                category.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        var result = await CreateService().CreateTransactionAsync(
            new CreateFinanceTransactionDto
            {
                TransactionDate = new DateOnly(2026, 9, 1),
                Type = FinanceTransactionType.Income,
                Amount = 2500,
                CategoryId = category.Id,
                Description = "  Salary  "
            });

        Assert.Equal("Salary", result.CategoryName);
        Assert.Equal("Salary", result.Description);
        _repository.Verify(item => item.AddTransactionAsync(
            It.Is<FinanceTransaction>(transaction =>
                transaction.UserId == UserId &&
                transaction.Type == FinanceTransactionType.Income &&
                transaction.Amount == 2500),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateTransactionAsync_RejectsMissingAndIncompatibleCategories()
    {
        var service = CreateService();
        var dto = new CreateFinanceTransactionDto
        {
            TransactionDate = new DateOnly(2026, 9, 1),
            Type = FinanceTransactionType.Income,
            Amount = 10,
            CategoryId = Guid.NewGuid()
        };

        _repository.Setup(item => item.GetCategoryByIdAsync(
                dto.CategoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((FinanceCategory?)null);
        await Assert.ThrowsAsync<ResourceNotFoundException>(
            () => service.CreateTransactionAsync(dto));

        var expenseCategory = Category(
            FinanceCategoryDefaults.FoodId,
            "Food",
            FinanceCategoryType.Expense,
            102);
        dto.CategoryId = expenseCategory.Id;
        _repository.Setup(item => item.GetCategoryByIdAsync(
                expenseCategory.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expenseCategory);

        await Assert.ThrowsAsync<ValidationException>(
            () => service.CreateTransactionAsync(dto));
        _repository.Verify(item => item.AddTransactionAsync(
            It.IsAny<FinanceTransaction>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetUpdateAndDelete_AreUserScoped()
    {
        var category = Category(
            FinanceCategoryDefaults.FoodId,
            "Food",
            FinanceCategoryType.Expense,
            102);
        var transaction = new FinanceTransaction(
            UserId,
            new DateOnly(2026, 9, 4),
            FinanceTransactionType.Expense,
            20,
            category.Id,
            "Lunch");
        _repository.Setup(item => item.GetTransactionByIdAsync(
                UserId, transaction.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction);
        _repository.Setup(item => item.GetCategoryByIdAsync(
                category.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        var service = CreateService();
        var result = await service.GetTransactionAsync(transaction.Id);
        Assert.Equal("Lunch", result.Description);

        var updated = await service.UpdateTransactionAsync(
            transaction.Id,
            new UpdateFinanceTransactionDto
            {
                TransactionDate = new DateOnly(2026, 9, 5),
                Type = FinanceTransactionType.Expense,
                Amount = 25,
                CategoryId = category.Id,
                Description = "Dinner"
            });
        Assert.Equal(25, updated.Amount);
        Assert.Equal("Dinner", updated.Description);
        _repository.Verify(item => item.UpdateTransactionAsync(
            It.Is<FinanceTransaction>(value =>
                value.UserId == UserId && value.Amount == 25),
            It.IsAny<CancellationToken>()), Times.Once);

        await service.DeleteTransactionAsync(transaction.Id);
        _repository.Verify(item => item.DeleteTransactionAsync(
            UserId, transaction.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TransactionWorkflows_RejectUnavailableUserAndMissingTransaction()
    {
        var unauthenticated = CreateService(false);
        await Assert.ThrowsAsync<CurrentUserUnavailableException>(
            () => unauthenticated.GetTransactionAsync(Guid.NewGuid()));

        _currentUser.Setup(item => item.IsAuthenticated).Returns(true);
        _currentUser.Setup(item => item.UserId).Returns(Guid.Empty);
        await Assert.ThrowsAsync<CurrentUserUnavailableException>(
            () => new FinanceService(
                _repository.Object,
                _currentUser.Object,
                _logger.Object).GetTransactionAsync(Guid.NewGuid()));

        _currentUser.Setup(item => item.UserId).Returns(UserId);
        _repository.Setup(item => item.GetTransactionByIdAsync(
                UserId, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((FinanceTransaction?)null);
        await Assert.ThrowsAsync<ResourceNotFoundException>(
            () => CreateService().GetTransactionAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task SummaryMethods_ReturnEmptyMonthAndCalculateYear()
    {
        var service = CreateService();

        _repository.Setup(item => item.GetTransactionsByMonthAsync(
                UserId,
                new DateOnly(2026, 9, 1),
                new DateOnly(2026, 10, 1),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<FinanceTransaction>());
        _repository.Setup(item => item.GetTransactionsByYearAsync(
                UserId,
                new DateOnly(2026, 1, 1),
                new DateOnly(2027, 1, 1),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                Transaction(
                    UserId,
                    new DateOnly(2026, 2, 1),
                    FinanceTransactionType.Income,
                    100,
                    FinanceCategoryDefaults.SalaryId,
                    "Salary")
            });

        var monthly = await service.GetMonthlySummaryAsync(2026, 9);
        Assert.Equal(0, monthly.TotalIncome);
        Assert.Equal(0, monthly.TotalExpenses);
        Assert.Empty(monthly.ExpenseCategories);
        Assert.Empty(monthly.Transactions);

        var yearly = await service.GetYearSummaryAsync(2026);
        Assert.Equal(100, yearly.TotalIncome);
        Assert.Equal(0, yearly.TotalExpenses);
        Assert.Equal(100, yearly.NetCashFlow);
    }

    [Fact]
    public async Task GetMonthlySummaryAsync_CalculatesTotalsCategoriesAndPreservesOrdering()
    {
        var salary = Category(
            FinanceCategoryDefaults.SalaryId,
            "Salary",
            FinanceCategoryType.Income,
            1);
        var food = Category(
            FinanceCategoryDefaults.FoodId,
            "Food",
            FinanceCategoryType.Expense,
            102);
        var transport = Category(
            FinanceCategoryDefaults.TransportId,
            "Transport",
            FinanceCategoryType.Expense,
            103);
        _repository.Setup(item => item.GetCategoriesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { salary, food, transport });

        var newest = Transaction(
            UserId,
            new DateOnly(2026, 9, 30),
            FinanceTransactionType.Expense,
            15,
            FinanceCategoryDefaults.FoodId,
            "Groceries");
        var income = Transaction(
            UserId,
            new DateOnly(2026, 9, 10),
            FinanceTransactionType.Income,
            1000,
            FinanceCategoryDefaults.SalaryId,
            "Salary");
        var sameCategory = Transaction(
            UserId,
            new DateOnly(2026, 9, 5),
            FinanceTransactionType.Expense,
            25,
            FinanceCategoryDefaults.FoodId,
            "Market");
        var otherCategory = Transaction(
            UserId,
            new DateOnly(2026, 9, 1),
            FinanceTransactionType.Expense,
            10,
            FinanceCategoryDefaults.TransportId,
            "Bus");
        var transactions = new[] { newest, income, sameCategory, otherCategory };
        _repository.Setup(item => item.GetTransactionsByMonthAsync(
                UserId,
                new DateOnly(2026, 9, 1),
                new DateOnly(2026, 10, 1),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(transactions);

        var result = await CreateService().GetMonthlySummaryAsync(2026, 9);

        Assert.Equal(1000, result.TotalIncome);
        Assert.Equal(50, result.TotalExpenses);
        Assert.Equal(950, result.NetCashFlow);
        Assert.Equal(
            new[] { FinanceCategoryDefaults.FoodId, FinanceCategoryDefaults.TransportId },
            result.ExpenseCategories.Select(category => category.CategoryId));
        Assert.Equal(
            new[] { 40m, 10m },
            result.ExpenseCategories.Select(category => category.TotalExpenses));
        Assert.Equal(50, result.ExpenseCategories.Sum(category => category.TotalExpenses));
        Assert.Equal(
            transactions.Select(transaction => transaction.Id),
            result.Transactions.Select(transaction => transaction.Id));
        Assert.Equal("Salary", result.Transactions[1].CategoryName);
        _repository.Verify(item => item.GetTransactionsByMonthAsync(
            UserId,
            new DateOnly(2026, 9, 1),
            new DateOnly(2026, 10, 1),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SummaryMethods_RejectInvalidDates()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => service.GetMonthlySummaryAsync(2026, 13));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => service.GetYearSummaryAsync(0));
    }

    private static FinanceCategory Category(
        Guid id,
        string name,
        FinanceCategoryType type,
        int sortOrder)
    {
        return new FinanceCategory(id, name, type, sortOrder);
    }

    private static FinanceTransaction Transaction(
        Guid userId,
        DateOnly date,
        FinanceTransactionType type,
        decimal amount,
        Guid categoryId,
        string description)
    {
        return new FinanceTransaction(
            userId,
            date,
            type,
            amount,
            categoryId,
            description);
    }
}
