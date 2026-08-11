namespace PersonalFinance.Core.Dtos.Dashboard;

public class CategorySpendDto
{
    public int? CategoryId { get; set; }
    public string CategoryName { get; set; } = "Uncategorized";
    public string? CategoryIcon { get; set; }
    public decimal Amount { get; set; }
}