using Bunit;
using LifeOS.Core.Abstractions;
using LifeOS.Core.DTOs;
using LifeOS.Core.Services;
using LifeOS.Web.Components.Pages;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using SettingsPage = LifeOS.Web.Components.Pages.Settings;

namespace LifeOS.Tests.Web.Settings;

public sealed class SettingsTests : IDisposable
{
    private readonly BunitContext _context = new();
    private readonly Mock<IUserSettingsService> _settings = new();
    private readonly Mock<IDateTimeProvider> _dateTime = new();

    public SettingsTests()
    {
        _dateTime
            .Setup(service => service.GetTimeZoneIds())
            .Returns(["UTC", "Europe/Bucharest"]);
        _settings
            .Setup(service => service.GetCurrentUserSettingsAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserSettingsDto
            {
                UserId = Guid.NewGuid(),
                TimeZoneId = "Europe/Bucharest",
                Currency = "USD"
            });

        _context.Services.AddSingleton(_settings.Object);
        _context.Services.AddSingleton(_dateTime.Object);
    }

    [Fact]
    public void FinanceCurrencyControl_RendersWithAccessibleLabelAndCurrentValue()
    {
        var cut = _context.Render<SettingsPage>();

        cut.WaitForAssertion(() =>
        {
            var currency = cut.Find("#currency");
            Assert.Equal("USD", currency.GetAttribute("value"));
            Assert.Equal("Currency", cut.Find("label[for='currency']").TextContent);
            Assert.Contains("Choose the currency used to display Finance amounts.", cut.Markup);
            Assert.DoesNotContain("budget", cut.Markup, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public void CurrencyCanBeChangedAndSaved()
    {
        var cut = _context.Render<SettingsPage>();
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll("#currency")));

        cut.Find("#currency").Change("EUR");
        cut.FindAll("button").Single(button =>
            button.TextContent.Contains("Save currency", StringComparison.Ordinal))
            .Click();

        cut.WaitForAssertion(() =>
        {
            _settings.Verify(service => service.UpdateCurrencyAsync(
                "EUR", It.IsAny<CancellationToken>()), Times.Once);
            Assert.Contains("Currency saved.", cut.Markup);
        });
    }

    [Fact]
    public void TimezoneControlStillRendersAlongsideFinanceControl()
    {
        var cut = _context.Render<SettingsPage>();

        cut.WaitForAssertion(() =>
        {
            Assert.NotEmpty(cut.FindAll("#time-zone"));
            Assert.NotEmpty(cut.FindAll("#currency"));
            Assert.Equal("Time zone", cut.Find("label[for='time-zone']").TextContent);
        });
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
