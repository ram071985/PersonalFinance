using PersonalFinance.Core.Dtos.Budgets;
using PersonalFinance.Core.Dtos.Transactions;

namespace PersonalFinance.Core.Dtos.Dashboard;

public class DashboardDto
{
    public decimal NetWorth { get; set; }
    public decimal MonthlyIncome { get; set; }
    public decimal MonthlyExpenses { get; set; }
    public decimal MonthlyNet { get; set; }
    public List<TransactionDto> RecentTransactions { get; set; } = new();

    /// <summary>Budgets over limit for the current month.</summary>
    public List<BudgetDto> OverBudget { get; set; } = new();
}