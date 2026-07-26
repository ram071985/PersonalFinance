using PersonalFinance.Core.Entities;

namespace PersonalFinance.Core.Interfaces;

public interface ITransactionService
{
    Task<IEnumerable<Transaction>> GetAllAsync();
    Task<IEnumerable<Transaction>> GetRecentAsync(int count = 10);
    Task<IEnumerable<Transaction>> GetByAccountIdAsync(int accountId);
    Task<Transaction?> GetByIdAsync(int id);
    Task<Transaction> CreateAsync(Transaction transaction);
    Task<bool> UpdateAsync(int id, Transaction input);
    Task DeleteAsync(int id);
}