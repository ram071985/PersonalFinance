using PersonalFinance.Core.Entities;
using PersonalFinance.Core.Enums;

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
    Task<bool> DeleteAsync(int id);
    Task<decimal> GetMonthlyIncomeAsync(int year, int month);
    Task<decimal> GetMonthlyExpensesAsync(int year, int month);
    Task<(IReadOnlyList<Transaction> Items, int Total)> GetPagedAsync(int page, int pageSize, TransactionType? type = null);
    Task<decimal> GetCategorySpentAsync(int categoryId, int year, int month);
    Task<IReadOnlyList<(int? CategoryId, string CategoryName, string? Icon, decimal Amount)>> GetCategorySpendAsync(int year, int month);
}