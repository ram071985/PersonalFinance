using PersonalFinance.Core.Dtos.Dashboard;
using PersonalFinance.Core.Interfaces;
using PersonalFinance.Core.Mappings;

namespace PersonalFinance.Infrastructure.Services;

public class DashboardService : IDashboardService
{
    private readonly IAccountRepository _accounts;
    private readonly ITransactionRepository _transactions;
    private readonly IBudgetService _budgets;

    public DashboardService(
        IAccountRepository accounts,
        ITransactionRepository transactions,
        IBudgetService budgets)
    {
        _accounts = accounts;
        _transactions = transactions;
        _budgets = budgets;
    }

    public async Task<DashboardDto> GetSummaryAsync()
    {
        var now = DateTime.UtcNow;

        var netWorth = await _accounts.GetTotalBalanceAsync();
        var monthlyIncome = await _transactions.GetMonthlyIncomeAsync(now.Year, now.Month);
        var monthlyExpenses = await _transactions.GetMonthlyExpensesAsync(now.Year, now.Month);
        var recent = await _transactions.GetRecentAsync(8);

        var monthBudgets = await _budgets.GetByMonthAsync(now.Year, now.Month);
        var over = monthBudgets.Where(b => b.IsOverBudget).ToList();

        return EntityMappings.ToDashboardDto(
            netWorth,
            monthlyIncome,
            monthlyExpenses,
            recent,
            over);
    }
}