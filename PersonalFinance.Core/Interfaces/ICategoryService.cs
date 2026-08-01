using PersonalFinance.Core.Common;
using PersonalFinance.Core.Dtos.Categories;
using PersonalFinance.Core.Enums;

namespace PersonalFinance.Core.Interfaces;

public interface ICategoryService
{
    Task<IEnumerable<CategoryDto>> GetAllAsync();
    Task<IEnumerable<CategoryDto>> GetByTypeAsync(CategoryType type);
    Task<CategoryDto?> GetByIdAsync(int id);
    Task<CategoryDto> CreateAsync(CreateCategoryRequest request);
    Task<Result> UpdateAsync(int id, UpdateCategoryRequest request);
    Task<bool> DeleteAsync(int id);
}