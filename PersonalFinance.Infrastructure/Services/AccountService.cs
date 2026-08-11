using PersonalFinance.Core.Common;
using PersonalFinance.Core.Dtos.Accounts;
using PersonalFinance.Core.Interfaces;
using PersonalFinance.Core.Mappings;

namespace PersonalFinance.Infrastructure.Services;

public class AccountService : IAccountService
{
    private readonly IAccountRepository _repo;

    public AccountService(IAccountRepository repo) => _repo = repo;

    public async Task<IEnumerable<AccountDto>> GetAllAsync() =>
        (await _repo.GetAllAsync()).ToDtoList();

    public async Task<PagedResult<AccountDto>> GetPagedAsync(int page = 1, int pageSize = 20)
    {
        var (items, total) = await _repo.GetPagedAsync(page, pageSize);
        return new PagedResult<AccountDto>
        {
            Items = items.ToDtoList(),
            TotalCount = total,
            Page = Math.Max(1, page),
            PageSize = Math.Clamp(pageSize, 1, 100)
        };
    }

    public async Task<AccountDto?> GetByIdAsync(int id)
    {
        var account = await _repo.GetByIdAsync(id);
        return account?.ToDto();
    }

    public async Task<AccountDto> CreateAsync(CreateAccountRequest request)
    {
        var created = await _repo.AddAsync(request.ToEntity());
        return created.ToDto();
    }

    public async Task<Result> UpdateAsync(int id, UpdateAccountRequest request)
    {
        var existing = await _repo.GetByIdAsync(id);
        if (existing is null)
            return Result.Fail("Account not found.");

        existing.ApplyUpdate(request);
        await _repo.UpdateAsync(existing);
        return Result.Ok();
    }

    public Task<bool> DeleteAsync(int id) => _repo.DeleteAsync(id);

    public Task<decimal> GetTotalBalanceAsync() => _repo.GetTotalBalanceAsync();
}