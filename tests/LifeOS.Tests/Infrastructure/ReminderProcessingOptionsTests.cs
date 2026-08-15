using System.ComponentModel.DataAnnotations;
using LifeOS.Infrastructure.Options;

namespace LifeOS.Tests.Infrastructure;

public sealed class ReminderProcessingOptionsTests
{
    [Fact]
    public void Defaults_AreFrozen()
    {
        var options = new ReminderProcessingOptions();

        Assert.Equal(100, options.BatchSize);
        Assert.Equal(3, options.AutomaticRetryAttempts);
        AssertValid(options);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(1000)]
    public void BatchSize_ValidBoundariesAreAccepted(int batchSize)
    {
        AssertValid(new ReminderProcessingOptions { BatchSize = batchSize });
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1001)]
    public void BatchSize_InvalidValuesAreRejected(int batchSize)
    {
        AssertInvalid(new ReminderProcessingOptions { BatchSize = batchSize });
    }

    [Fact]
    public void NegativeRetryAttempts_AreRejected()
    {
        AssertInvalid(new ReminderProcessingOptions
        {
            AutomaticRetryAttempts = -1
        });
    }

    private static void AssertValid(ReminderProcessingOptions options)
    {
        Assert.Empty(Validate(options));
    }

    private static void AssertInvalid(ReminderProcessingOptions options)
    {
        Assert.NotEmpty(Validate(options));
    }

    private static IList<ValidationResult> Validate(
        ReminderProcessingOptions options)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(
            options,
            new ValidationContext(options),
            results,
            validateAllProperties: true);
        return results;
    }
}
