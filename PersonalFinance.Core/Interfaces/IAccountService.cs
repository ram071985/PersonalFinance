using PersonalFinance.Core.Common;
using PersonalFinance.Core.Dtos.Accounts;

namespace PersonalFinance.Core.Interfaces;

public interface IAccountService
{
    Task<IEnumerable<AccountDto>> GetAllAsync();
    Task<PagedResult<AccountDto>> GetPagedAsync(int page = 1, int pageSize = 20);
    Task<AccountDto?> GetByIdAsync(int id);
    Task<AccountDto> CreateAsync(CreateAccountRequest request);
    Task<Result> UpdateAsync(int id, UpdateAccountRequest request);
    Task<bool> DeleteAsync(int id);
    Task<decimal> GetTotalBalanceAsync();
}