using Microsoft.EntityFrameworkCore;
using PersonalFinance.Core.Entities;
using PersonalFinance.Core.Interfaces;
using PersonalFinance.Infrastructure.Data;

namespace PersonalFinance.Infrastructure.Repositories;

public class BudgetRepository : IBudgetRepository
{
    private readonly AppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public BudgetRepository(AppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    private string UserId =>
        _currentUser.UserId
        ?? throw new UnauthorizedAccessException("Authenticated user is required.");

    public async Task<IEnumerable<Budget>> GetAllAsync() =>
        await _db.Budgets
            .Include(b => b.Category)
            .Where(b => b.UserId == UserId)
            .OrderByDescending(b => b.Year)
            .ThenByDescending(b => b.Month)
            .ThenBy(b => b.Category.Name)
            .ToListAsync();

    public async Task<IEnumerable<Budget>> GetByMonthAsync(int year, int month) =>
        await _db.Budgets
            .Include(b => b.Category)
            .Where(b => b.UserId == UserId && b.Year == year && b.Month == month)
            .OrderBy(b => b.Category.Name)
            .ToListAsync();

    public async Task<Budget?> GetByIdAsync(int id) =>
        await _db.Budgets
            .Include(b => b.Category)
            .FirstOrDefaultAsync(b => b.Id == id && b.UserId == UserId);

    public async Task<Budget?> GetByCategoryAndMonthAsync(int categoryId, int year, int month) =>
        await _db.Budgets
            .FirstOrDefaultAsync(b =>
                b.UserId == UserId &&
                b.CategoryId == categoryId &&
                b.Year == year &&
                b.Month == month);

    public async Task<Budget> AddAsync(Budget budget)
    {
        await EnsureCategoryOwnershipAsync(budget.CategoryId);

        budget.UserId = UserId;
        budget.CreatedAt = DateTime.UtcNow;
        _db.Budgets.Add(budget);
        await _db.SaveChangesAsync();
        return budget;
    }

    public async Task UpdateAsync(Budget budget)
    {
        if (budget.UserId != UserId)
            throw new UnauthorizedAccessException("Cannot update another user's budget.");

        await EnsureCategoryOwnershipAsync(budget.CategoryId);

        _db.Budgets.Update(budget);
        await _db.SaveChangesAsync();
    }

    private async Task EnsureCategoryOwnershipAsync(int categoryId)
    {
        var owns = await _db.Categories
            .AnyAsync(c => c.Id == categoryId && c.UserId == UserId && c.IsActive);

        if (!owns)
            throw new UnauthorizedAccessException("Category does not belong to the current user.");
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var budget = await GetByIdAsync(id);
        if (budget is null) return false;

        _db.Budgets.Remove(budget);
        await _db.SaveChangesAsync();
        return true;
    }
}
