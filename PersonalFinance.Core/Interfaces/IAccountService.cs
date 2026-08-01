using PersonalFinance.Core.Common;
using PersonalFinance.Core.Dtos.Accounts;

namespace PersonalFinance.Core.Interfaces;

public interface IAccountService
{
    Task<IEnumerable<AccountDto>> GetAllAsync();
    Task<AccountDto?> GetByIdAsync(int id);
    Task<AccountDto> CreateAsync(CreateAccountRequest request);
    Task<Result> UpdateAsync(int id, UpdateAccountRequest request);
    Task<bool> DeleteAsync(int id);
    Task<decimal> GetTotalBalanceAsync();
}