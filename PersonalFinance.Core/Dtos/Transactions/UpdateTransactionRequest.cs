using System.ComponentModel.DataAnnotations;
using PersonalFinance.Core.Enums;

namespace PersonalFinance.Core.Dtos.Transactions;

public class UpdateTransactionRequest : IValidatableObject
{
    [Range(1, int.MaxValue, ErrorMessage = "Account is required.")]
    public int AccountId { get; set; }

    public int? CategoryId { get; set; }

    public int? TransferToAccountId { get; set; }

    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than zero.")]
    public decimal Amount { get; set; }

    [EnumDataType(typeof(TransactionType), ErrorMessage = "Invalid transaction type.")]
    public TransactionType Type { get; set; }

    [Required(ErrorMessage = "Description is required.")]
    [MaxLength(200, ErrorMessage = "Description must be 200 characters or fewer.")]
    public string Description { get; set; } = string.Empty;

    [MaxLength(500, ErrorMessage = "Notes must be 500 characters or fewer.")]
    public string? Notes { get; set; }

    [Required(ErrorMessage = "Date is required.")]
    public DateTime Date { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Type == TransactionType.Transfer)
        {
            if (TransferToAccountId is null or <= 0)
            {
                yield return new ValidationResult(
                    "Transfer destination account is required for transfers.",
                    new[] { nameof(TransferToAccountId) });
            }
            else if (TransferToAccountId == AccountId)
            {
                yield return new ValidationResult(
                    "Cannot transfer to the same account.",
                    new[] { nameof(TransferToAccountId) });
            }
        }
        else if (TransferToAccountId is not null)
        {
            yield return new ValidationResult(
                "Transfer destination is only valid for transfer transactions.",
                new[] { nameof(TransferToAccountId) });
        }

        if (Type is TransactionType.Income or TransactionType.Expense)
        {
            if (CategoryId is null or <= 0)
            {
                yield return new ValidationResult(
                    "Category is required for income and expense transactions.",
                    new[] { nameof(CategoryId) });
            }
        }
    }
}
