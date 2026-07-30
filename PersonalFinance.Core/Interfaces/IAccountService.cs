using PersonalFinance.Core.Dtos;
using PersonalFinance.Core.Dtos.Accounts;

namespace PersonalFinance.Core.Interfaces;

public interface IAccountService
{
    Task<IEnumerable<AccountDto>> GetAllAsync();
    Task<AccountDto?> GetByIdAsync(int id);
    Task<AccountDto> CreateAsync(CreateAccountRequest request);
    Task<bool> UpdateAsync(int id, UpdateAccountRequest request);
    Task DeleteAsync(int id);
    Task<decimal> GetTotalBalanceAsync();
}