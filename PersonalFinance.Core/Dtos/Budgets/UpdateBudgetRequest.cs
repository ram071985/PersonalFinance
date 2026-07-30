namespace PersonalFinance.Core.Dtos.Budgets;

public class UpdateBudgetRequest
{
    public int CategoryId { get; set; }
    public decimal Amount { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public string? Notes { get; set; }
}