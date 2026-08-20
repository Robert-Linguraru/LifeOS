using Bunit;
using LifeOS.Core.DTOs;
using LifeOS.Core.DTOs.Dashboard;
using LifeOS.Core.Services;
using LifeOS.Web.Components.Dashboard;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace LifeOS.Tests.Web.Dashboard;

public sealed class FinanceWidgetTests : IDisposable
{
    private readonly BunitContext _context = new();
    private readonly Mock<IDashboardService> _dashboard = new();
    private readonly Mock<IUserSettingsService> _settings = new();

    public FinanceWidgetTests()
    {
        _settings.Setup(service => service.GetCurrentUserSettingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserSettingsDto { Currency = "EUR", TimeZoneId = "UTC" });
        _context.Services.AddSingleton(_dashboard.Object);
        _context.Services.AddSingleton(_settings.Object);
    }

    [Fact]
    public void LoadingState_IsDisplayedUntilServiceCompletes()
    {
        var completion = new TaskCompletionSource<DashboardFinanceWidgetDto>();
        _dashboard.Setup(service => service.GetFinanceWidgetAsync(It.IsAny<CancellationToken>()))
            .Returns(completion.Task);

        var cut = _context.Render<FinanceWidget>();

        Assert.Contains("Loading Finance...", cut.Markup);
        completion.SetResult(new DashboardFinanceWidgetDto());
    }

    [Fact]
    public void EmptyState_ShowsMessageAndActions()
    {
        SetupWidget(new DashboardFinanceWidgetDto());

        var cut = _context.Render<FinanceWidget>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("No transactions recorded this month.", cut.Markup);
            Assert.Contains("/finance", cut.Markup);
            Assert.Contains("/finance/transactions/new?type=income", cut.Markup);
        });
    }

    [Fact]
    public void PopulatedState_RendersCurrencyAmountsAndLargestCategory()
    {
        SetupWidget(new DashboardFinanceWidgetDto
        {
            HasTransactions = true,
            TotalIncome = 3000,
            TotalExpenses = 1450,
            LargestExpenseCategory = "Housing/Rent"
        });

        var cut = _context.Render<FinanceWidget>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("EUR 3,000.00", cut.Markup);
            Assert.Contains("EUR 1,450.00", cut.Markup);
            Assert.Contains("EUR 1,550.00", cut.Markup);
            Assert.Contains("Housing/Rent", cut.Markup);
            Assert.Contains("Add expense", cut.Markup);
        });
    }

    [Fact]
    public void ErrorState_ProvidesRetryAndRecovers()
    {
        var shouldFail = true;
        _dashboard.Setup(service => service.GetFinanceWidgetAsync(It.IsAny<CancellationToken>()))
            .Returns(() => shouldFail
                ? Task.FromException<DashboardFinanceWidgetDto>(new InvalidOperationException())
                : Task.FromResult(new DashboardFinanceWidgetDto
                {
                    HasTransactions = true,
                    TotalIncome = 10
                }));

        var cut = _context.Render<FinanceWidget>();

        cut.WaitForAssertion(() => Assert.Contains("Finance could not be loaded", cut.Markup));
        shouldFail = false;
        cut.Find("button").Click();

        cut.WaitForAssertion(() => Assert.Contains("EUR 10.00", cut.Markup));
    }

    private void SetupWidget(DashboardFinanceWidgetDto widget)
    {
        _dashboard.Setup(service => service.GetFinanceWidgetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(widget);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
