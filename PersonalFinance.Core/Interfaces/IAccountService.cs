using PersonalFinance.Core.Entities;

namespace PersonalFinance.Core.Interfaces;

public interface IAccountService
{
    Task<IEnumerable<Account>> GetAllAsync();
    Task<Account?> GetByIdAsync(int id);
    Task<Account> CreateAsync(Account account);
    Task<bool> UpdateAsync(int id, Account input);
    Task DeleteAsync(int id);
    Task<decimal> GetTotalBalanceAsync();
}