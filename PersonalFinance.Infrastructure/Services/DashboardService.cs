using PersonalFinance.Core.Dtos;
using PersonalFinance.Core.Dtos.Dashboard;
using PersonalFinance.Core.Interfaces;
using PersonalFinance.Core.Mappings;

namespace PersonalFinance.Infrastructure.Services;

public class DashboardService : IDashboardService
{
    private readonly IAccountRepository _accounts;
    private readonly ITransactionRepository _transactions;

    public DashboardService(
        IAccountRepository accounts,
        ITransactionRepository transactions)
    {
        _accounts = accounts;
        _transactions = transactions;
    }

    public async Task<DashboardDto> GetSummaryAsync()
    {
        var now = DateTime.UtcNow;

        var netWorth = await _accounts.GetTotalBalanceAsync();
        var monthlyIncome = await _transactions.GetMonthlyIncomeAsync(now.Year, now.Month);
        var monthlyExpenses = await _transactions.GetMonthlyExpensesAsync(now.Year, now.Month);
        var recent = await _transactions.GetRecentAsync(8);

        return EntityMappings.ToDashboardDto(
            netWorth,
            monthlyIncome,
            monthlyExpenses,
            recent);
    }
}