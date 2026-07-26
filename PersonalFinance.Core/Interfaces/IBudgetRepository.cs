using PersonalFinance.Core.Entities;

namespace PersonalFinance.Core.Interfaces;

public interface IBudgetRepository
{
    Task<IEnumerable<Budget>> GetAllAsync();
    Task<IEnumerable<Budget>> GetByMonthAsync(int year, int month);
    Task<Budget?> GetByIdAsync(int id);
    Task<Budget?> GetByCategoryAndMonthAsync(int categoryId, int year, int month);
    Task<Budget> AddAsync(Budget budget);
    Task UpdateAsync(Budget budget);
    Task DeleteAsync(int id);
}