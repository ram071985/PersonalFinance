using System.ComponentModel.DataAnnotations;
using PersonalFinance.Core.Enums;

namespace PersonalFinance.Web.Models;

public class TransactionFormModel
{
    public int? Id { get; set; }

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Select an account")]
    public int AccountId { get; set; }

    public int? CategoryId { get; set; }

    public int? TransferToAccountId { get; set; }

    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than zero")]
    public decimal Amount { get; set; }

    public TransactionType Type { get; set; } = TransactionType.Expense;

    [Required(ErrorMessage = "Description is required")]
    [StringLength(200)]
    public string Description { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Notes { get; set; }

    public DateTime Date { get; set; } = DateTime.Today;

    public bool IsSaving { get; set; }
    public string? ErrorMessage { get; set; }
}