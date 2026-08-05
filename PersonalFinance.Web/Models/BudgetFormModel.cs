using System.ComponentModel.DataAnnotations;

namespace PersonalFinance.Web.Models;

public class BudgetFormModel
{
    public int? Id { get; set; }

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Select a category")]
    public int CategoryId { get; set; }

    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than zero")]
    public decimal Amount { get; set; }

    [Range(2000, 2100)]
    public int Year { get; set; } = DateTime.Today.Year;

    [Range(1, 12)]
    public int Month { get; set; } = DateTime.Today.Month;

    [StringLength(300)]
    public string? Notes { get; set; }

    public bool IsSaving { get; set; }
    public string? ErrorMessage { get; set; }
}