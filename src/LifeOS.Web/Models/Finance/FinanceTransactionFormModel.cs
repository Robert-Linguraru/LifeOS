using System.ComponentModel.DataAnnotations;
using LifeOS.Core.Enums.Finance;

namespace LifeOS.Web.Models.Finance;

public sealed class FinanceTransactionFormModel : IValidatableObject
{
    [Required(ErrorMessage = "Enter an amount.")]
    [Range(typeof(decimal), "0.01", "79228162514264337593543950335", ErrorMessage = "Amount must be greater than zero.")]
    public decimal Amount { get; set; }

    [Required(ErrorMessage = "Select a date.")]
    public DateOnly? TransactionDate { get; set; }

    [Required(ErrorMessage = "Select a category.")]
    public Guid? CategoryId { get; set; }

    public FinanceTransactionType Type { get; set; }

    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
    public string? Description { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (CategoryId is null || CategoryId == Guid.Empty)
        {
            yield return new ValidationResult(
                "Select a category.",
                [nameof(CategoryId)]);
        }
    }
}
