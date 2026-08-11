using PersonalFinance.Core.Common;
using PersonalFinance.Core.Dtos.Transactions;

namespace PersonalFinance.Core.Interfaces;

public interface ITransactionService
{
    Task<IEnumerable<TransactionDto>> GetAllAsync();
    Task<PagedResult<TransactionDto>> GetPagedAsync(int page = 1, int pageSize = 20);
    Task<IEnumerable<TransactionDto>> GetRecentAsync(int count = 10);
    Task<IEnumerable<TransactionDto>> GetByAccountIdAsync(int accountId);
    Task<TransactionDto?> GetByIdAsync(int id);
    Task<TransactionDto> CreateAsync(CreateTransactionRequest request);
    Task<Result> UpdateAsync(int id, UpdateTransactionRequest request);
    Task<bool> DeleteAsync(int id);

    Task<BankStatementImportResult> ImportBankStatementAsync(
        int accountId,
        int? defaultExpenseCategoryId,
        int? defaultIncomeCategoryId,
        Stream csvStream,
        string? fileName = null);
}