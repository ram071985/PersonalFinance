using PersonalFinance.Core.Common;
using PersonalFinance.Core.Dtos.Budgets;
using PersonalFinance.Core.Interfaces;
using PersonalFinance.Core.Mappings;

namespace PersonalFinance.Infrastructure.Services;

public class BudgetService : IBudgetService
{
    private readonly IBudgetRepository _repo;
    private readonly ITransactionRepository _transactions;

    public BudgetService(IBudgetRepository repo, ITransactionRepository transactions)
    {
        _repo = repo;
        _transactions = transactions;
    }

    public async Task<IEnumerable<BudgetDto>> GetAllAsync()
    {
        var budgets = (await _repo.GetAllAsync()).ToDtoList();
        foreach (var b in budgets)
            b.Spent = await _transactions.GetCategorySpentAsync(b.CategoryId, b.Year, b.Month);
        return budgets;
    }

    public async Task<IEnumerable<BudgetDto>> GetByMonthAsync(int year, int month)
    {
        var budgets = (await _repo.GetByMonthAsync(year, month)).ToDtoList();
        foreach (var b in budgets)
            b.Spent = await _transactions.GetCategorySpentAsync(b.CategoryId, b.Year, b.Month);
        return budgets;
    }

    public async Task<BudgetDto?> GetByIdAsync(int id)
    {
        var budget = await _repo.GetByIdAsync(id);
        if (budget is null) return null;
        var dto = budget.ToDto();
        dto.Spent = await _transactions.GetCategorySpentAsync(dto.CategoryId, dto.Year, dto.Month);
        return dto;
    }

    public async Task<BudgetDto> CreateAsync(CreateBudgetRequest request)
    {
        var created = await _repo.AddAsync(request.ToEntity());
        var full = await _repo.GetByIdAsync(created.Id);
        var dto = full!.ToDto();
        dto.Spent = await _transactions.GetCategorySpentAsync(dto.CategoryId, dto.Year, dto.Month);
        return dto;
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
