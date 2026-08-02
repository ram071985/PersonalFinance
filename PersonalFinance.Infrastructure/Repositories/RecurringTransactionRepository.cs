using Microsoft.EntityFrameworkCore;
using PersonalFinance.Core.Entities;
using PersonalFinance.Core.Interfaces;
using PersonalFinance.Infrastructure.Data;

namespace PersonalFinance.Infrastructure.Repositories;

public class RecurringTransactionRepository : IRecurringTransactionRepository
{
    private readonly AppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public RecurringTransactionRepository(AppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    private string UserId =>
        _currentUser.UserId ?? throw new UnauthorizedAccessException("Authenticated user is required.");

    public async Task<IEnumerable<RecurringTransaction>> GetAllAsync() =>
        await _db.RecurringTransactions
            .Include(r => r.Account)
            .Include(r => r.Category)
            .Where(r => r.UserId == UserId)
            .OrderBy(r => r.DayOfMonth)
            .ToListAsync();

    public async Task<RecurringTransaction?> GetByIdAsync(int id) =>
        await _db.RecurringTransactions
            .Include(r => r.Account)
            .Include(r => r.Category)
            .FirstOrDefaultAsync(r => r.Id == id && r.UserId == UserId);

    public async Task<RecurringTransaction> AddAsync(RecurringTransaction entity)
    {
        entity.UserId = UserId;
        entity.CreatedAt = DateTime.UtcNow;
        _db.RecurringTransactions.Add(entity);
        await _db.SaveChangesAsync();
        return entity;
    }

    public async Task UpdateAsync(RecurringTransaction entity)
    {
        await _db.SaveChangesAsync();
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var entity = await GetByIdAsync(id);
        if (entity is null) return false;
        entity.IsActive = false;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<IReadOnlyList<RecurringTransaction>> GetDueForDateAsync(DateTime dateUtc)
    {
        var day = dateUtc.Day;
        // Cap at 28 for month-end safety (templates only allow 1-28)
        if (day > 28) day = 28;

        return await _db.RecurringTransactions
            .IgnoreQueryFilters()
            .Where(r => r.IsActive
                        && r.DayOfMonth == day
                        && r.StartDate <= dateUtc.Date
                        && (r.EndDate == null || r.EndDate >= dateUtc.Date)
                        && (r.LastGeneratedDate == null
                            || r.LastGeneratedDate.Value.Year != dateUtc.Year
                            || r.LastGeneratedDate.Value.Month != dateUtc.Month))
            .ToListAsync();
    }
}
