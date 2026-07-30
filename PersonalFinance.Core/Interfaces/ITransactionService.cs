using PersonalFinance.Core.Dtos;
using PersonalFinance.Core.Dtos.Transactions;

namespace PersonalFinance.Core.Interfaces;

public interface ITransactionService
{
    Task<IEnumerable<TransactionDto>> GetAllAsync();
    Task<IEnumerable<TransactionDto>> GetRecentAsync(int count = 10);
    Task<IEnumerable<TransactionDto>> GetByAccountIdAsync(int accountId);
    Task<TransactionDto?> GetByIdAsync(int id);
    Task<TransactionDto> CreateAsync(CreateTransactionRequest request);
    Task<bool> UpdateAsync(int id, UpdateTransactionRequest request);
    Task DeleteAsync(int id);
}