using Microsoft.EntityFrameworkCore;
using PersonalFinance.Core.Entities;
using PersonalFinance.Core.Interfaces;
using PersonalFinance.Infrastructure.Data;

namespace PersonalFinance.Infrastructure.Repositories;

public class AccountRepository : IAccountRepository
{
    private readonly AppDbContext _db;

    public AccountRepository(AppDbContext db) => _db = db;

    public async Task<IEnumerable<Account>> GetAllAsync() =>
        await _db.Accounts
            .Where(a => a.IsActive)
            .OrderBy(a => a.Name)
            .ToListAsync();

    public async Task<Account?> GetByIdAsync(int id) =>
        await _db.Accounts.FindAsync(id);

    public async Task<Account> AddAsync(Account account)
    {
        _db.Accounts.Add(account);
        await _db.SaveChangesAsync();
        return account;
    }

    public async Task UpdateAsync(Account account)
    {
        account.UpdatedAt = DateTime.UtcNow;
        _db.Accounts.Update(account);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var account = await _db.Accounts.FindAsync(id);
        if (account is null) return;
        account.IsActive = false;
        account.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    public async Task<decimal> GetTotalBalanceAsync() =>
        await _db.Accounts
            .Where(a => a.IsActive)
            .SumAsync(a => a.Balance);
}