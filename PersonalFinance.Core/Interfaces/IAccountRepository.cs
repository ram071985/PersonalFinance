using PersonalFinance.Core.Entities;

namespace PersonalFinance.Core.Interfaces;

public interface IAccountRepository
{
    Task<IEnumerable<Account>> GetAllAsync();
    Task<Account?> GetByIdAsync(int id);
    Task<Account> AddAsync(Account account);
    Task UpdateAsync(Account account);
    Task<bool> DeleteAsync(int id);
    Task<decimal> GetTotalBalanceAsync();
    Task<(IReadOnlyList<Account> Items, int Total)> GetPagedAsync(int page, int pageSize);
}