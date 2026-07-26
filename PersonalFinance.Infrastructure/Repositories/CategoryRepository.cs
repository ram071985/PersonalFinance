using Microsoft.EntityFrameworkCore;
using PersonalFinance.Core.Entities;
using PersonalFinance.Core.Enums;
using PersonalFinance.Core.Interfaces;
using PersonalFinance.Infrastructure.Data;

namespace PersonalFinance.Infrastructure.Repositories;

public class CategoryRepository : ICategoryRepository
{
    private readonly AppDbContext _db;

    public CategoryRepository(AppDbContext db) => _db = db;

    public async Task<IEnumerable<Category>> GetAllAsync() =>
        await _db.Categories
            .Where(c => c.IsActive)
            .OrderBy(c => c.Type)
            .ThenBy(c => c.Name)
            .ToListAsync();

    public async Task<IEnumerable<Category>> GetByTypeAsync(CategoryType type) =>
        await _db.Categories
            .Where(c => c.IsActive && c.Type == type)
            .OrderBy(c => c.Name)
            .ToListAsync();

    public async Task<Category?> GetByIdAsync(int id) =>
        await _db.Categories.FindAsync(id);

    public async Task<Category> AddAsync(Category category)
    {
        _db.Categories.Add(category);
        await _db.SaveChangesAsync();
        return category;
    }

    public async Task UpdateAsync(Category category)
    {
        _db.Categories.Update(category);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var category = await _db.Categories.FindAsync(id);
        if (category is null) return;
        category.IsActive = false;
        await _db.SaveChangesAsync();
    }
}