using LifeOS.Core.Enums;
using LifeOS.Core.Enums.Xp;
using LifeOS.Core.Progression;

namespace LifeOS.Tests.Core.Xp;

public sealed class XpRulesTests
{
    public static IEnumerable<object[]> QuestXpCases =>
        new[]
        {
            new object[] { EstimatedTime.Under15Minutes, FrictionLevel.Low, 50 },
            new object[] { EstimatedTime.Under15Minutes, FrictionLevel.Medium, 75 },
            new object[] { EstimatedTime.Under15Minutes, FrictionLevel.High, 100 },
            new object[] { EstimatedTime.Between15And30Minutes, FrictionLevel.Low, 100 },
            new object[] { EstimatedTime.Between15And30Minutes, FrictionLevel.Medium, 150 },
            new object[] { EstimatedTime.Between15And30Minutes, FrictionLevel.High, 200 },
            new object[] { EstimatedTime.Between30And60Minutes, FrictionLevel.Low, 150 },
            new object[] { EstimatedTime.Between30And60Minutes, FrictionLevel.Medium, 225 },
            new object[] { EstimatedTime.Between30And60Minutes, FrictionLevel.High, 300 },
            new object[] { EstimatedTime.Over60Minutes, FrictionLevel.Low, 200 },
            new object[] { EstimatedTime.Over60Minutes, FrictionLevel.Medium, 300 },
            new object[] { EstimatedTime.Over60Minutes, FrictionLevel.High, 400 }
        };

    public static IEnumerable<object[]> EchelonCases =>
        new[]
        {
            new object[] { 1, Echelon.Iron },
            new object[] { 9, Echelon.Iron },
            new object[] { 10, Echelon.Bronze },
            new object[] { 19, Echelon.Bronze },
            new object[] { 20, Echelon.Silver },
            new object[] { 29, Echelon.Silver },
            new object[] { 30, Echelon.Gold },
            new object[] { 39, Echelon.Gold },
            new object[] { 40, Echelon.Platinum },
            new object[] { 49, Echelon.Platinum },
            new object[] { 50, Echelon.Onyx },
            new object[] { 74, Echelon.Onyx },
            new object[] { 75, Echelon.Radiant },
            new object[] { 99, Echelon.Radiant },
            new object[] { 100, Echelon.Apex },
            new object[] { 124, Echelon.Apex },
            new object[] { 125, Echelon.Celestial },
            new object[] { 149, Echelon.Celestial },
            new object[] { 150, Echelon.Immortal },
            new object[] { 174, Echelon.Immortal },
            new object[] { 175, Echelon.Abyssal },
            new object[] { 199, Echelon.Abyssal },
            new object[] { 200, Echelon.Ascendant },
            new object[] { 1000, Echelon.Ascendant }
        };

    [Theory]
    [MemberData(nameof(QuestXpCases))]
    public void CalculateQuestXp_ShouldReturnCanonicalMatrix(
        EstimatedTime estimatedTime,
        FrictionLevel frictionLevel,
        int expected)
    {
        Assert.Equal(
            expected,
            XpRules.CalculateQuestXp(estimatedTime, frictionLevel));
    }

    [Theory]
    [InlineData(0, 100, 100)]
    [InlineData(400, 100, 100)]
    [InlineData(450, 100, 50)]
    [InlineData(499, 100, 1)]
    [InlineData(500, 100, 0)]
    [InlineData(0, 0, 0)]
    public void CalculateActualQuestXp_ShouldRespectDailyCap(
        int alreadyAwarded,
        int rawAward,
        int expected)
    {
        Assert.Equal(
            expected,
            XpRules.CalculateActualQuestXp(alreadyAwarded, rawAward));
    }

    [Fact]
    public void CalculateQuestXp_ShouldRejectUndefinedEnums()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            XpRules.CalculateQuestXp((EstimatedTime)99, FrictionLevel.Low));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            XpRules.CalculateQuestXp(EstimatedTime.Under15Minutes, (FrictionLevel)99));
    }

    [Theory]
    [InlineData(-1, 100)]
    [InlineData(501, 100)]
    [InlineData(0, -1)]
    public void CalculateActualQuestXp_ShouldRejectInvalidInputs(
        int alreadyAwarded,
        int rawAward)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            XpRules.CalculateActualQuestXp(alreadyAwarded, rawAward));
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(179, 1)]
    [InlineData(180, 2)]
    [InlineData(181, 2)]
    [InlineData(2699, 9)]
    [InlineData(2700, 10)]
    [InlineData(2701, 10)]
    [InlineData(12599, 24)]
    [InlineData(12600, 25)]
    [InlineData(12601, 25)]
    [InlineData(44099, 49)]
    [InlineData(44100, 50)]
    [InlineData(44101, 50)]
    [InlineData(163349, 99)]
    [InlineData(163350, 100)]
    [InlineData(163351, 100)]
    public void CalculateLevel_ShouldRespectThresholdBoundaries(
        long totalLifetimeXp,
        int expectedLevel)
    {
        Assert.Equal(expectedLevel, XpRules.CalculateLevel(totalLifetimeXp));
    }

    [Fact]
    public void CalculateLevel_ShouldHandleLongMaxValueWithoutOverflow()
    {
        var level = XpRules.CalculateLevel(long.MaxValue);

        Assert.True(level > 100_000);
        Assert.True(XpRules.GetLifetimeXpThreshold(level) <= long.MaxValue);
        Assert.Throws<OverflowException>(() =>
            XpRules.GetLifetimeXpThreshold(level + 1));
    }

    [Fact]
    public void CalculateLevel_ShouldRejectNegativeLifetimeXp()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            XpRules.CalculateLevel(-1));
    }

    [Theory]
    [MemberData(nameof(EchelonCases))]
    public void CalculateEchelon_ShouldUseCanonicalBoundaries(
        int level,
        Echelon expected)
    {
        Assert.Equal(expected, XpRules.CalculateEchelon(level));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void CalculateEchelon_ShouldRejectInvalidLevels(int level)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            XpRules.CalculateEchelon(level));
    }

    [Theory]
    [InlineData(1, 0)]
    [InlineData(2, 180)]
    [InlineData(10, 2700)]
    [InlineData(25, 12600)]
    [InlineData(50, 44100)]
    [InlineData(100, 163350)]
    public void GetLifetimeXpThreshold_ShouldReturnCanonicalValues(
        int level,
        long expectedThreshold)
    {
        Assert.Equal(expectedThreshold, XpRules.GetLifetimeXpThreshold(level));
    }

    [Fact]
    public void Enums_ShouldRetainDocumentedNumericValues()
    {
        Assert.Equal(0, (int)XpSource.QuestCompletion);
        Assert.Equal(1, (int)XpSource.DailyScore);
        Assert.Equal(2, (int)XpSource.StreakBonus);
        Assert.Equal(3, (int)XpSource.ManualAdjustment);
        Assert.Equal(4, (int)XpSource.System);

        Assert.Equal(0, (int)XpSourceType.Task);
        Assert.Equal(1, (int)XpSourceType.Habit);
        Assert.Equal(2, (int)XpSourceType.DailyScore);
        Assert.Equal(3, (int)XpSourceType.Streak);

        Assert.Equal(0, (int)Echelon.Iron);
        Assert.Equal(11, (int)Echelon.Ascendant);
    }
}
