using PersonalFinance.Core.Dtos;
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

    public async Task<bool> UpdateAsync(int id, UpdateAccountRequest request)
    {
        var existing = await _repo.GetByIdAsync(id);
        if (existing is null) return false;

        existing.ApplyUpdate(request);
        await _repo.UpdateAsync(existing);
        return true;
    }

    public Task DeleteAsync(int id) => _repo.DeleteAsync(id);

    public Task<decimal> GetTotalBalanceAsync() => _repo.GetTotalBalanceAsync();
}