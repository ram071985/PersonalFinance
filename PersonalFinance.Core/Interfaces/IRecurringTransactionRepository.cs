using PersonalFinance.Core.Entities;

namespace PersonalFinance.Core.Interfaces;

public interface IRecurringTransactionRepository
{
    Task<IEnumerable<RecurringTransaction>> GetAllAsync();
    Task<RecurringTransaction?> GetByIdAsync(int id);
    Task<RecurringTransaction> AddAsync(RecurringTransaction entity);
    Task UpdateAsync(RecurringTransaction entity);
    Task<bool> DeleteAsync(int id);
    /// <summary>All active templates due on the given date (ignores tenancy filter — system use only).</summary>
    Task<IReadOnlyList<RecurringTransaction>> GetDueForDateAsync(DateTime dateUtc);

}