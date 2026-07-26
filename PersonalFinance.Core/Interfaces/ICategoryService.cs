using PersonalFinance.Core.Entities;
using PersonalFinance.Core.Enums;

namespace PersonalFinance.Core.Interfaces;

public interface ICategoryService
{
    Task<IEnumerable<Category>> GetAllAsync();
    Task<IEnumerable<Category>> GetByTypeAsync(CategoryType type);
    Task<Category?> GetByIdAsync(int id);
    Task<Category> CreateAsync(Category category);
    Task<bool> UpdateAsync(int id, Category input);
    Task DeleteAsync(int id);
}