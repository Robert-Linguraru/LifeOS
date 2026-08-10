using System.ComponentModel.DataAnnotations;
using LifeOS.Core.Constants;
using LifeOS.Core.Enums.Tasks;
using LifeOS.Core.Enums;

namespace LifeOS.Web.Models.Tasks;

public sealed class TaskFormModel
{
    [Required(ErrorMessage = "Title is required.")]
    [MaxLength(
        TaskConstants.TitleMaxLength,
        ErrorMessage = "Title cannot exceed 200 characters.")]
    public string Title { get; set; } = string.Empty;

    [MaxLength(
        TaskConstants.DescriptionMaxLength,
        ErrorMessage = "Description cannot exceed 2000 characters.")]
    public string? Description { get; set; }

    public DateOnly? DueDate { get; set; }

    public TimeOnly? DueTime { get; set; }

    public TaskPriority Priority { get; set; } =
        TaskPriority.Low;

    public TaskCategory Category { get; set; } =
        TaskCategory.Personal;

    public EstimatedTime EstimatedTime { get; set; } =
        EstimatedTime.Under15Minutes;

    public FrictionLevel FrictionLevel { get; set; } =
        FrictionLevel.Low;
}