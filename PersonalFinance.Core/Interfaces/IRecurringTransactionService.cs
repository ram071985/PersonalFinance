using PersonalFinance.Core.Dtos.Recurring;
using PersonalFinance.Core.Dtos.Transactions;

namespace PersonalFinance.Core.Interfaces;

public interface IRecurringTransactionService
{
    Task<IEnumerable<RecurringTransactionDto>> GetAllAsync();
    Task<RecurringTransactionDto> CreateAsync(CreateRecurringTransactionRequest request);
    Task<bool> DeleteAsync(int id);
    /// <summary>Creates a real Transaction from the template for "today" if due.</summary>
    Task<TransactionDto?> GenerateDueAsync(int id);
    Task<int> GenerateAllDueAsync(DateTime? asOfUtc = null);

}