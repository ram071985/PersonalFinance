using Microsoft.EntityFrameworkCore;
using PersonalFinance.Core.Entities;
using PersonalFinance.Core.Interfaces;
using PersonalFinance.Infrastructure.Data;

namespace PersonalFinance.Infrastructure.Repositories;

public class AccountRepository : IAccountRepository
{
    private readonly AppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public AccountRepository(AppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    private string UserId =>
        _currentUser.UserId
        ?? throw new UnauthorizedAccessException("Authenticated user is required.");

    public async Task<IEnumerable<Account>> GetAllAsync() =>
        await _db.Accounts
            .Where(a => a.UserId == UserId && a.IsActive)
            .OrderBy(a => a.Name)
            .ToListAsync();

    public async Task<Account?> GetByIdAsync(int id) =>
        await _db.Accounts
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == UserId);

    public async Task<Account> AddAsync(Account account)
    {
        account.UserId = UserId;
        account.CreatedAt = DateTime.UtcNow;
        _db.Accounts.Add(account);
        await _db.SaveChangesAsync();
        return account;
    }

    public async Task UpdateAsync(Account account)
    {
        // Ownership already enforced by GetByIdAsync in the service layer.
        // Extra guard: never allow cross-user write.
        if (account.UserId != UserId)
            throw new UnauthorizedAccessException("Cannot update another user's account.");

        account.UpdatedAt = DateTime.UtcNow;
        _db.Accounts.Update(account);
        await _db.SaveChangesAsync();
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var account = await GetByIdAsync(id);
        if (account is null) return false;

        account.IsActive = false;
        account.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<decimal> GetTotalBalanceAsync() =>
        await _db.Accounts
            .Where(a => a.UserId == UserId && a.IsActive)
            .SumAsync(a => a.Balance);

    public async Task<(IReadOnlyList<Account> Items, int Total)> GetPagedAsync(int page, int pageSize)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _db.Accounts.Where(a => a.UserId == UserId && a.IsActive);
        var total = await query.CountAsync();
        var items = await query
            .OrderBy(a => a.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        return (items, total);
    }
}
