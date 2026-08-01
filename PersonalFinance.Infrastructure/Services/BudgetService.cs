using PersonalFinance.Core.Common;
using PersonalFinance.Core.Dtos.Budgets;
using PersonalFinance.Core.Interfaces;
using PersonalFinance.Core.Mappings;

namespace PersonalFinance.Infrastructure.Services;

public class BudgetService : IBudgetService
{
    private readonly IBudgetRepository _repo;

    public BudgetService(IBudgetRepository repo) => _repo = repo;

    public async Task<IEnumerable<BudgetDto>> GetAllAsync() =>
        (await _repo.GetAllAsync()).ToDtoList();

    public async Task<IEnumerable<BudgetDto>> GetByMonthAsync(int year, int month) =>
        (await _repo.GetByMonthAsync(year, month)).ToDtoList();

    public async Task<BudgetDto?> GetByIdAsync(int id)
    {
        var budget = await _repo.GetByIdAsync(id);
        return budget?.ToDto();
    }

    public async Task<BudgetDto> CreateAsync(CreateBudgetRequest request)
    {
        var created = await _repo.AddAsync(request.ToEntity());
        // re-fetch so CategoryName / CategoryIcon are populated
        var full = await _repo.GetByIdAsync(created.Id);
        return full!.ToDto();
    }

    public async Task<Result> UpdateAsync(int id, UpdateBudgetRequest request)
    {
        var existing = await _repo.GetByIdAsync(id);
        if (existing is null)
            return Result.Fail("Budget not found.");

        existing.ApplyUpdate(request);
        await _repo.UpdateAsync(existing);
        return Result.Ok();
    }

    public Task<bool> DeleteAsync(int id) => _repo.DeleteAsync(id);
}