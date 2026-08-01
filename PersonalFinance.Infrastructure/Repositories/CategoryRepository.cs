using Microsoft.EntityFrameworkCore;
using PersonalFinance.Core.Entities;
using PersonalFinance.Core.Enums;
using PersonalFinance.Core.Interfaces;
using PersonalFinance.Infrastructure.Data;

namespace PersonalFinance.Infrastructure.Repositories;

public class CategoryRepository : ICategoryRepository
{
    private readonly AppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public CategoryRepository(AppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    private string UserId =>
        _currentUser.UserId
        ?? throw new UnauthorizedAccessException("Authenticated user is required.");

    public async Task<IEnumerable<Category>> GetAllAsync() =>
        await _db.Categories
            .Where(c => c.UserId == UserId && c.IsActive)
            .OrderBy(c => c.Type)
            .ThenBy(c => c.Name)
            .ToListAsync();

    public async Task<IEnumerable<Category>> GetByTypeAsync(CategoryType type) =>
        await _db.Categories
            .Where(c => c.UserId == UserId && c.IsActive && c.Type == type)
            .OrderBy(c => c.Name)
            .ToListAsync();

    public async Task<Category?> GetByIdAsync(int id) =>
        await _db.Categories
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == UserId);

    public async Task<Category> AddAsync(Category category)
    {
        category.UserId = UserId;
        category.CreatedAt = DateTime.UtcNow;
        _db.Categories.Add(category);
        await _db.SaveChangesAsync();
        return category;
    }

    public async Task UpdateAsync(Category category)
    {
        if (category.UserId != UserId)
            throw new UnauthorizedAccessException("Cannot update another user's category.");

        _db.Categories.Update(category);
        await _db.SaveChangesAsync();
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var category = await GetByIdAsync(id);
        if (category is null) return false;

        category.IsActive = false;
        await _db.SaveChangesAsync();
        return true;
    }
}
