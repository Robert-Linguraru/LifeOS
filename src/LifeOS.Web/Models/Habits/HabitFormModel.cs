using System.ComponentModel.DataAnnotations;
using LifeOS.Core.Constants;
using LifeOS.Core.Enums;
using LifeOS.Core.Enums.Habits;

namespace LifeOS.Web.Models.Habits;

public sealed class HabitFormModel : IValidatableObject
{
    [Required(ErrorMessage = "Name is required.")]
    [MaxLength(
        HabitConstants.NameMaxLength,
        ErrorMessage = "Name cannot exceed 200 characters.")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(
        HabitConstants.DescriptionMaxLength,
        ErrorMessage = "Description cannot exceed 2000 characters.")]
    public string? Description { get; set; }

    public HabitFrequency Frequency { get; set; } = HabitFrequency.Daily;

    public HabitTargetType TargetType { get; set; } = HabitTargetType.Binary;

    public decimal? TargetQuantity { get; set; }

    [MaxLength(
        HabitConstants.TargetUnitMaxLength,
        ErrorMessage = "Target unit cannot exceed 50 characters.")]
    public string? TargetUnit { get; set; }

    public EstimatedTime EstimatedTime { get; set; } =
        EstimatedTime.Under15Minutes;

    public FrictionLevel FrictionLevel { get; set; } = FrictionLevel.Low;

    public IEnumerable<ValidationResult> Validate(
        ValidationContext validationContext)
    {
        if (TargetType != HabitTargetType.Quantity)
        {
            yield break;
        }

        if (!TargetQuantity.HasValue || TargetQuantity.Value <= 0)
        {
            yield return new ValidationResult(
                "A positive target quantity is required.",
                [nameof(TargetQuantity)]);
        }

        if (string.IsNullOrWhiteSpace(TargetUnit))
        {
            yield return new ValidationResult(
                "A target unit is required.",
                [nameof(TargetUnit)]);
        }
    }
}
