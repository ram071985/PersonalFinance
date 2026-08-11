using PersonalFinance.Core.Entities;
using PersonalFinance.Core.Enums;

namespace PersonalFinance.Core.Interfaces;

public interface ICategoryRepository
{
    Task<IEnumerable<Category>> GetAllAsync();
    Task<IEnumerable<Category>> GetByTypeAsync(CategoryType type);
    Task<Category?> GetByIdAsync(int id);
    Task<Category> AddAsync(Category category);
    Task UpdateAsync(Category category);
    Task<bool> DeleteAsync(int id);
}