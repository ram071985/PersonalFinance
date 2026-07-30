namespace PersonalFinance.Core.Dtos.Budgets;

public class CreateBudgetRequest
{
    public int CategoryId { get; set; }
    public decimal Amount { get; set; }
    public int Year { get; set; } = DateTime.Today.Year;
    public int Month { get; set; } = DateTime.Today.Month;
    public string? Notes { get; set; }
}