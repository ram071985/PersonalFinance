using PersonalFinance.Core.Entities;
using PersonalFinance.Core.Interfaces;

namespace PersonalFinance.Infrastructure.Services;

public class BudgetService : IBudgetService
{
    private readonly IBudgetRepository _repo;

    public BudgetService(IBudgetRepository repo) => _repo = repo;

    public Task<IEnumerable<Budget>> GetAllAsync() => _repo.GetAllAsync();

    public Task<IEnumerable<Budget>> GetByMonthAsync(int year, int month) => _repo.GetByMonthAsync(year, month);

    public Task<Budget?> GetByIdAsync(int id) => _repo.GetByIdAsync(id);

    public Task<Budget> CreateAsync(Budget budget) => _repo.AddAsync(budget);

    public async Task<bool> UpdateAsync(int id, Budget input)
    {
        var existing = await _repo.GetByIdAsync(id);
        if (existing is null) return false;

        existing.CategoryId = input.CategoryId;
        existing.Amount = input.Amount;
        existing.Year = input.Year;
        existing.Month = input.Month;
        existing.Notes = input.Notes;

        await _repo.UpdateAsync(existing);
        return true;
    }

    public Task DeleteAsync(int id) => _repo.DeleteAsync(id);
}