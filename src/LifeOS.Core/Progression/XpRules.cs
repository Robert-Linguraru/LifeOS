using LifeOS.Core.Constants;
using LifeOS.Core.Enums;
using LifeOS.Core.Enums.Xp;

namespace LifeOS.Core.Progression;

public static class XpRules
{
    private const int LevelIncrementXp = 30;

    public static int CalculateQuestXp(
        EstimatedTime estimatedTime,
        FrictionLevel frictionLevel)
    {
        var baseXp = estimatedTime switch
        {
            EstimatedTime.Under15Minutes => 50,
            EstimatedTime.Between15And30Minutes => 100,
            EstimatedTime.Between30And60Minutes => 150,
            EstimatedTime.Over60Minutes => 200,
            _ => throw new ArgumentOutOfRangeException(nameof(estimatedTime), estimatedTime, "Estimated time is invalid.")
        };

        var multiplier = frictionLevel switch
        {
            FrictionLevel.Low => 1.0m,
            FrictionLevel.Medium => 1.5m,
            FrictionLevel.High => 2.0m,
            _ => throw new ArgumentOutOfRangeException(nameof(frictionLevel), frictionLevel, "Friction level is invalid.")
        };

        return checked((int)Math.Round(
            baseXp * multiplier,
            0,
            MidpointRounding.AwayFromZero));
    }

    public static int CalculateActualQuestXp(
        int alreadyAwarded,
        int rawAward)
    {
        if (alreadyAwarded < 0 || alreadyAwarded > XpConstants.DailyQuestXpCap)
        {
            throw new ArgumentOutOfRangeException(
                nameof(alreadyAwarded),
                alreadyAwarded,
                "Already awarded XP must be between zero and the daily Quest XP cap.");
        }

        if (rawAward < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(rawAward),
                rawAward,
                "Raw Quest XP cannot be negative.");
        }

        var remaining = XpConstants.DailyQuestXpCap - alreadyAwarded;
        return Math.Min(rawAward, remaining);
    }

    public static int CalculateLevel(long totalLifetimeXp)
    {
        if (totalLifetimeXp < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(totalLifetimeXp),
                totalLifetimeXp,
                "Lifetime XP cannot be negative.");
        }

        var low = 1;
        var high = int.MaxValue;

        while (low < high)
        {
            var midpoint = low + (int)(((long)high - low + 1) / 2);

            if (IsThresholdAtMost(midpoint, totalLifetimeXp))
            {
                low = midpoint;
            }
            else
            {
                high = midpoint - 1;
            }
        }

        return low;
    }

    public static long GetLifetimeXpThreshold(int level)
    {
        ValidateLevel(level);

        var firstFactor = (long)level - 1;
        var secondFactor = (long)level + 10;
        var multiplier = (long)LevelIncrementXp / 2;

        if (firstFactor != 0 &&
            (firstFactor > long.MaxValue / multiplier ||
             secondFactor > long.MaxValue / (multiplier * firstFactor)))
        {
            throw new OverflowException("The level threshold exceeds Int64 capacity.");
        }

        return multiplier * firstFactor * secondFactor;
    }

    public static Echelon CalculateEchelon(int level)
    {
        ValidateLevel(level);

        return level switch
        {
            <= 9 => Echelon.Iron,
            <= 19 => Echelon.Bronze,
            <= 29 => Echelon.Silver,
            <= 39 => Echelon.Gold,
            <= 49 => Echelon.Platinum,
            <= 74 => Echelon.Onyx,
            <= 99 => Echelon.Radiant,
            <= 124 => Echelon.Apex,
            <= 149 => Echelon.Celestial,
            <= 174 => Echelon.Immortal,
            <= 199 => Echelon.Abyssal,
            _ => Echelon.Ascendant
        };
    }

    private static bool IsThresholdAtMost(int level, long totalLifetimeXp)
    {
        var firstFactor = (long)level - 1;
        var secondFactor = (long)level + 10;
        var quotient = totalLifetimeXp / (LevelIncrementXp / 2);

        return firstFactor == 0 ||
            (firstFactor <= quotient &&
             secondFactor <= quotient / firstFactor);
    }

    private static void ValidateLevel(int level)
    {
        if (level < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(level),
                level,
                "Level must be at least 1.");
        }
    }
}
