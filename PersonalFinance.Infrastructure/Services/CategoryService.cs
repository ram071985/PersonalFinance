using PersonalFinance.Core.Common;
using PersonalFinance.Core.Dtos.Categories;
using PersonalFinance.Core.Enums;
using PersonalFinance.Core.Interfaces;
using PersonalFinance.Core.Mappings;

namespace PersonalFinance.Infrastructure.Services;

public class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _repo;

    public CategoryService(ICategoryRepository repo) => _repo = repo;

    public async Task<IEnumerable<CategoryDto>> GetAllAsync() =>
        (await _repo.GetAllAsync()).ToDtoList();

    public async Task<IEnumerable<CategoryDto>> GetByTypeAsync(CategoryType type) =>
        (await _repo.GetByTypeAsync(type)).ToDtoList();

    public async Task<CategoryDto?> GetByIdAsync(int id)
    {
        var category = await _repo.GetByIdAsync(id);
        return category?.ToDto();
    }

    public async Task<CategoryDto> CreateAsync(CreateCategoryRequest request)
    {
        var created = await _repo.AddAsync(request.ToEntity());
        return created.ToDto();
    }

    public async Task<Result> UpdateAsync(int id, UpdateCategoryRequest request)
    {
        var existing = await _repo.GetByIdAsync(id);
        if (existing is null)
            return Result.Fail("Category not found.");

        existing.ApplyUpdate(request);
        await _repo.UpdateAsync(existing);
        return Result.Ok();
    }

    public Task<bool> DeleteAsync(int id) => _repo.DeleteAsync(id);
}