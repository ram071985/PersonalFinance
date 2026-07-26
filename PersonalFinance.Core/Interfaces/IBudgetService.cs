using PersonalFinance.Core.Entities;

namespace PersonalFinance.Core.Interfaces;

public interface IBudgetService
{
    Task<IEnumerable<Budget>> GetAllAsync();
    Task<IEnumerable<Budget>> GetByMonthAsync(int year, int month);
    Task<Budget?> GetByIdAsync(int id);
    Task<Budget> CreateAsync(Budget budget);
    Task<bool> UpdateAsync(int id, Budget input);
    Task DeleteAsync(int id);
}