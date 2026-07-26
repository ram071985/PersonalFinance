using PersonalFinance.Core.Interfaces;

namespace PersonalFinance.Infrastructure.Services;

public class DashboardService : IDashboardService
{
    private readonly IAccountRepository _accounts;
    private readonly ITransactionRepository _transactions;

    public DashboardService(IAccountRepository accounts, ITransactionRepository transactions)
    {
        _accounts = accounts;
        _transactions = transactions;
    }

    public async Task<DashboardSummary> GetSummaryAsync()
    {
        var now = DateTime.UtcNow;
        var income = await _transactions.GetMonthlyIncomeAsync(now.Year, now.Month);
        var expenses = await _transactions.GetMonthlyExpensesAsync(now.Year, now.Month);

        return new DashboardSummary
        {
            NetWorth = await _accounts.GetTotalBalanceAsync(),
            MonthlyIncome = income,
            MonthlyExpenses = expenses,
            MonthlyNet = income - expenses,
            RecentTransactions = await _transactions.GetRecentAsync(8)
        };
    }
}