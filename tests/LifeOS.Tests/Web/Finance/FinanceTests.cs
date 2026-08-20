using Bunit;
using Microsoft.AspNetCore.Components;
using LifeOS.Core.Abstractions;
using LifeOS.Core.DTOs;
using LifeOS.Core.DTOs.Finance;
using LifeOS.Core.Enums.Finance;
using LifeOS.Core.Services;
using LifeOS.Web.Components.Pages;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using FinancePage = LifeOS.Web.Components.Pages.Finance;

namespace LifeOS.Tests.Web.Finance;

public sealed class FinanceTests : IDisposable
{
    private readonly BunitContext _context = new();
    private readonly Mock<IFinanceService> _finance = new();
    private readonly Mock<IUserSettingsService> _settings = new();

    public FinanceTests()
    {
        _settings.Setup(service => service.GetCurrentUserSettingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserSettingsDto { Currency = "EUR", TimeZoneId = "UTC" });
        _finance.Setup(service => service.GetMonthlySummaryAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FinanceSummaryDto
            {
                Year = 2026,
                Month = 8,
                TotalIncome = 1500,
                TotalExpenses = 320,
                ExpenseCategories =
                [
                    new FinanceCategorySummaryDto
                    {
                        CategoryId = Guid.NewGuid(),
                        CategoryName = "Food",
                        TotalExpenses = 320
                    }
                ],
                Transactions =
                [
                    new FinanceTransactionDto
                    {
                        Id = Guid.NewGuid(),
                        TransactionDate = new DateOnly(2026, 8, 10),
                        Type = FinanceTransactionType.Expense,
                        Amount = 320,
                        CategoryId = Guid.NewGuid(),
                        CategoryName = "Food",
                        Description = "Groceries"
                    }
                ]
            });
        _finance.Setup(service => service.GetYearSummaryAsync(
                It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FinanceYearSummaryDto
            {
                Year = 2026,
                TotalIncome = 1500,
                TotalExpenses = 320
            });

        _context.Services.AddSingleton(_finance.Object);
        _context.Services.AddSingleton(_settings.Object);
    }

    [Fact]
    public void FinancePage_RendersCurrencySummaryCategoriesAndTransactions()
    {
        var cut = _context.Render<FinancePage>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("EUR 1,500.00", cut.Markup);
            Assert.Contains("EUR 320.00", cut.Markup);
            Assert.Contains("Food", cut.Markup);
            Assert.Contains("Groceries", cut.Markup);
            Assert.Contains("Year", cut.Markup);
        });
    }

    [Fact]
    public void FinancePage_NavigatesBetweenMonths()
    {
        var cut = _context.Render<FinancePage>();
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll("button")));

        cut.FindAll("button").Single(button => button.TextContent.Contains("Previous month")).Click();

        cut.WaitForAssertion(() =>
            _finance.Verify(service => service.GetMonthlySummaryAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.AtLeast(2)));
    }

    [Fact]
    public void FinancePage_ProvidesIncomeAndExpenseActions()
    {
        var cut = _context.Render<FinancePage>();
        cut.WaitForAssertion(() => Assert.Contains("Add income", cut.Markup));

        cut.FindAll("button").Single(button => button.TextContent.Contains("Add income")).Click();
        Assert.EndsWith("/finance/transactions/new?type=income", _context.Services.GetRequiredService<NavigationManager>().Uri);
    }

    [Fact]
    public void FinancePage_EmptyMonthShowsUsefulEmptyState()
    {
        _finance.Setup(service => service.GetMonthlySummaryAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FinanceSummaryDto { Year = 2026, Month = 8 });

        var cut = _context.Render<FinancePage>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("No expenses in this month.", cut.Markup);
            Assert.Contains("No transactions in this month.", cut.Markup);
            Assert.Contains("EUR 0.00", cut.Markup);
        });
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}

public sealed class FinanceTransactionEditorTests : IDisposable
{
    private readonly BunitContext _context = new();
    private readonly Mock<IFinanceService> _finance = new();

    public FinanceTransactionEditorTests()
    {
        _finance.Setup(service => service.GetCategoriesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new FinanceCategoryDto
                {
                    Id = Guid.NewGuid(),
                    Name = "Salary",
                    Type = FinanceCategoryType.Income
                },
                new FinanceCategoryDto
                {
                    Id = Guid.NewGuid(),
                    Name = "Food",
                    Type = FinanceCategoryType.Expense
                }
            });
        _context.Services.AddSingleton(_finance.Object);
    }

    [Fact]
    public void NewExpenseEditor_PreselectsExpenseAndFiltersCategories()
    {
        _context.Services.GetRequiredService<NavigationManager>()
            .NavigateTo("/finance/transactions/new?type=expense");
        var cut = _context.Render<FinanceTransactionEditor>();

        cut.WaitForAssertion(() =>
        {
            Assert.Equal("Expense", cut.Find("#finance-transaction-type").GetAttribute("value"));
            Assert.Contains("Food", cut.Find("#finance-transaction-category").InnerHtml);
            Assert.DoesNotContain("Salary", cut.Find("#finance-transaction-category").InnerHtml);
            Assert.Contains("currency", cut.Markup, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public void NewIncomeEditor_PreselectsIncomeAndRequiresPositiveAmount()
    {
        _context.Services.GetRequiredService<NavigationManager>()
            .NavigateTo("/finance/transactions/new?type=income");
        var cut = _context.Render<FinanceTransactionEditor>();

        cut.WaitForAssertion(() => Assert.Equal("Income", cut.Find("#finance-transaction-type").GetAttribute("value")));
        cut.Find("#finance-transaction-amount").Change("0");
        cut.Find("button[type='submit']").Click();

        cut.WaitForAssertion(() => Assert.Contains("Amount must be greater than zero.", cut.Markup));
        _finance.Verify(service => service.CreateTransactionAsync(
            It.IsAny<CreateFinanceTransactionDto>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
