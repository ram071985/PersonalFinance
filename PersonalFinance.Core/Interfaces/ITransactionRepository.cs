using PersonalFinance.Core.Entities;

namespace PersonalFinance.Core.Interfaces;

public interface ITransactionRepository
{
    Task<IEnumerable<Transaction>> GetAllAsync();
    Task<IEnumerable<Transaction>> GetByAccountIdAsync(int accountId);
    Task<IEnumerable<Transaction>> GetRecentAsync(int count = 10);
    Task<IEnumerable<Transaction>> GetByDateRangeAsync(DateTime from, DateTime to);
    Task<Transaction?> GetByIdAsync(int id);
    Task<Transaction> AddAsync(Transaction transaction);
    Task UpdateAsync(Transaction transaction);
    Task DeleteAsync(int id);
    Task<decimal> GetMonthlyIncomeAsync(int year, int month);
    Task<decimal> GetMonthlyExpensesAsync(int year, int month);
}