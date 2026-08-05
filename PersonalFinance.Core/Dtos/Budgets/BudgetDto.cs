namespace PersonalFinance.Core.Dtos.Budgets;

public class BudgetDto
{
    public int Id { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string? CategoryIcon { get; set; }
    public decimal Amount { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }

    /// <summary>Sum of expense transactions in this category for the budget month.</summary>
    public decimal Spent { get; set; }
    public decimal Remaining => Amount - Spent;
    public decimal PercentUsed => Amount <= 0 ? 0 : Math.Round(Spent / Amount * 100m, 1);
    public bool IsOverBudget => Spent > Amount;
}