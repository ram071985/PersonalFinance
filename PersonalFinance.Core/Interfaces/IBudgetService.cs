using PersonalFinance.Core.Common;
using PersonalFinance.Core.Dtos.Budgets;

namespace PersonalFinance.Core.Interfaces;

public interface IBudgetService
{
    Task<IEnumerable<BudgetDto>> GetAllAsync();
    Task<IEnumerable<BudgetDto>> GetByMonthAsync(int year, int month);
    Task<BudgetDto?> GetByIdAsync(int id);
    Task<BudgetDto> CreateAsync(CreateBudgetRequest request);
    Task<Result> UpdateAsync(int id, UpdateBudgetRequest request);
    Task<bool> DeleteAsync(int id);
}