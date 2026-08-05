using System.ComponentModel.DataAnnotations;

namespace PersonalFinance.Core.Dtos.Budgets;

public class UpdateBudgetRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Category is required.")]
    public int CategoryId { get; set; }

    [Range(0.01, double.MaxValue, ErrorMessage = "Budget amount must be greater than zero.")]
    public decimal Amount { get; set; }

    [Range(2000, 2100, ErrorMessage = "Year must be between 2000 and 2100.")]
    public int Year { get; set; }

    [Range(1, 12, ErrorMessage = "Month must be between 1 and 12.")]
    public int Month { get; set; }

    [MaxLength(500, ErrorMessage = "Notes must be 500 characters or fewer.")]
    public string? Notes { get; set; }
}