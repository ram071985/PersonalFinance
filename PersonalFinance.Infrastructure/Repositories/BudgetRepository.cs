using Microsoft.EntityFrameworkCore;
using PersonalFinance.Core.Entities;
using PersonalFinance.Core.Interfaces;
using PersonalFinance.Infrastructure.Data;

namespace PersonalFinance.Infrastructure.Repositories;

public class BudgetRepository : IBudgetRepository
{
    private readonly AppDbContext _db;

    public BudgetRepository(AppDbContext db) => _db = db;

    public async Task<IEnumerable<Budget>> GetAllAsync() =>
        await _db.Budgets
            .Include(b => b.Category)
            .OrderByDescending(b => b.Year)
            .ThenByDescending(b => b.Month)
            .ThenBy(b => b.Category.Name)
            .ToListAsync();

    public async Task<IEnumerable<Budget>> GetByMonthAsync(int year, int month) =>
        await _db.Budgets
            .Include(b => b.Category)
            .Where(b => b.Year == year && b.Month == month)
            .OrderBy(b => b.Category.Name)
            .ToListAsync();

    public async Task<Budget?> GetByIdAsync(int id) =>
        await _db.Budgets
            .Include(b => b.Category)
            .FirstOrDefaultAsync(b => b.Id == id);

    public async Task<Budget?> GetByCategoryAndMonthAsync(int categoryId, int year, int month) =>
        await _db.Budgets
            .FirstOrDefaultAsync(b => b.CategoryId == categoryId && b.Year == year && b.Month == month);

    public async Task<Budget> AddAsync(Budget budget)
    {
        _db.Budgets.Add(budget);
        await _db.SaveChangesAsync();
        return budget;
    }

    public async Task UpdateAsync(Budget budget)
    {
        _db.Budgets.Update(budget);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var budget = await _db.Budgets.FindAsync(id);
        if (budget is null) return;
        _db.Budgets.Remove(budget);
        await _db.SaveChangesAsync();
    }
}