using PersonalFinance.Core.Entities;
using PersonalFinance.Core.Interfaces;

namespace PersonalFinance.Infrastructure.Services;

public class AccountService : IAccountService
{
    private readonly IAccountRepository _repo;

    public AccountService(IAccountRepository repo) => _repo = repo;

    public Task<IEnumerable<Account>> GetAllAsync() => _repo.GetAllAsync();

    public Task<Account?> GetByIdAsync(int id) => _repo.GetByIdAsync(id);

    public Task<Account> CreateAsync(Account account) => _repo.AddAsync(account);

    public async Task<bool> UpdateAsync(int id, Account input)
    {
        var existing = await _repo.GetByIdAsync(id);
        if (existing is null) return false;

        existing.Name = input.Name;
        existing.Type = input.Type;
        existing.Balance = input.Balance;
        existing.Institution = input.Institution;
        existing.Notes = input.Notes;
        existing.IsActive = input.IsActive;

        await _repo.UpdateAsync(existing);
        return true;
    }

    public Task DeleteAsync(int id) => _repo.DeleteAsync(id);

    public Task<decimal> GetTotalBalanceAsync() => _repo.GetTotalBalanceAsync();
}