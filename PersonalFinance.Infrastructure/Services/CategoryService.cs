using PersonalFinance.Core.Entities;
using PersonalFinance.Core.Enums;
using PersonalFinance.Core.Interfaces;

namespace PersonalFinance.Infrastructure.Services;

public class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _repo;

    public CategoryService(ICategoryRepository repo) => _repo = repo;

    public Task<IEnumerable<Category>> GetAllAsync() => _repo.GetAllAsync();

    public Task<IEnumerable<Category>> GetByTypeAsync(CategoryType type) => _repo.GetByTypeAsync(type);

    public Task<Category?> GetByIdAsync(int id) => _repo.GetByIdAsync(id);

    public Task<Category> CreateAsync(Category category) => _repo.AddAsync(category);

    public async Task<bool> UpdateAsync(int id, Category input)
    {
        var existing = await _repo.GetByIdAsync(id);
        if (existing is null) return false;

        existing.Name = input.Name;
        existing.Type = input.Type;
        existing.Icon = input.Icon;
        existing.Color = input.Color;
        existing.IsActive = input.IsActive;

        await _repo.UpdateAsync(existing);
        return true;
    }

    public Task DeleteAsync(int id) => _repo.DeleteAsync(id);
}