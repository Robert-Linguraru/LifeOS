using System.Globalization;
using LifeOS.Core.Progression;

namespace LifeOS.Tests.Core.Xp;

public sealed class XpIdempotencyKeyFactoryTests
{
    private static readonly Guid SourceId =
        Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

    [Fact]
    public void ForTask_ShouldReturnDeterministicDFormatKey()
    {
        Assert.Equal(
            "TaskComplete:aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee",
            XpIdempotencyKeyFactory.ForTask(SourceId));

        Assert.Equal(
            XpIdempotencyKeyFactory.ForTask(SourceId),
            XpIdempotencyKeyFactory.ForTask(SourceId));
    }

    [Fact]
    public void ForHabit_ShouldReturnDeterministicIsoDateKey()
    {
        var completionDate = new DateOnly(2026, 8, 12);

        Assert.Equal(
            "HabitComplete:aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee:2026-08-12",
            XpIdempotencyKeyFactory.ForHabit(SourceId, completionDate));
    }

    [Fact]
    public void Keys_ShouldChangeForDifferentSourceIdsOrHabitDates()
    {
        var otherId = Guid.Parse("ffffffff-1111-2222-3333-444444444444");
        var date = new DateOnly(2026, 8, 12);

        Assert.NotEqual(
            XpIdempotencyKeyFactory.ForTask(SourceId),
            XpIdempotencyKeyFactory.ForTask(otherId));
        Assert.NotEqual(
            XpIdempotencyKeyFactory.ForHabit(SourceId, date),
            XpIdempotencyKeyFactory.ForHabit(SourceId, date.AddDays(1)));
        Assert.NotEqual(
            XpIdempotencyKeyFactory.ForHabit(SourceId, date),
            XpIdempotencyKeyFactory.ForHabit(otherId, date));
    }

    [Fact]
    public void Keys_ShouldBeCultureIndependent()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        var originalUiCulture = CultureInfo.CurrentUICulture;

        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("tr-TR");
            CultureInfo.CurrentUICulture = new CultureInfo("tr-TR");

            Assert.Equal(
                "HabitComplete:aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee:2026-08-12",
                XpIdempotencyKeyFactory.ForHabit(
                    SourceId,
                    new DateOnly(2026, 8, 12)));
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
        }
    }

    [Fact]
    public void Keys_ShouldRejectEmptySourceIds()
    {
        Assert.Throws<ArgumentException>(() =>
            XpIdempotencyKeyFactory.ForTask(Guid.Empty));

        Assert.Throws<ArgumentException>(() =>
            XpIdempotencyKeyFactory.ForHabit(
                Guid.Empty,
                new DateOnly(2026, 8, 12)));
    }
}
