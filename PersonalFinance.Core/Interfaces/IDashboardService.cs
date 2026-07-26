using PersonalFinance.Core.Entities;

namespace PersonalFinance.Core.Interfaces;

public interface IDashboardService
{
    Task<DashboardSummary> GetSummaryAsync();
}

public class DashboardSummary
{
    public decimal NetWorth { get; set; }
    public decimal MonthlyIncome { get; set; }
    public decimal MonthlyExpenses { get; set; }
    public decimal MonthlyNet { get; set; }
    public IEnumerable<Transaction> RecentTransactions { get; set; } = [];
}