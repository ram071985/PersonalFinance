using PersonalFinance.Core.Dtos;
using PersonalFinance.Core.Interfaces;
using PersonalFinance.Core.Mappings;
using PersonalFinance.Core.Dtos.Transactions;

namespace PersonalFinance.Infrastructure.Services;

public class TransactionService : ITransactionService
{
    private readonly ITransactionRepository _repo;

    public TransactionService(ITransactionRepository repo) => _repo = repo;

    public async Task<IEnumerable<TransactionDto>> GetAllAsync() =>
        (await _repo.GetAllAsync()).ToDtoList();

    public async Task<IEnumerable<TransactionDto>> GetRecentAsync(int count = 10) =>
        (await _repo.GetRecentAsync(count)).ToDtoList();

    public async Task<IEnumerable<TransactionDto>> GetByAccountIdAsync(int accountId) =>
        (await _repo.GetByAccountIdAsync(accountId)).ToDtoList();

    public async Task<TransactionDto?> GetByIdAsync(int id)
    {
        var transaction = await _repo.GetByIdAsync(id);
        return transaction?.ToDto();
    }

    public async Task<TransactionDto> CreateAsync(CreateTransactionRequest request)
    {
        var created = await _repo.AddAsync(request.ToEntity());
        // re-fetch with includes for AccountName / CategoryName / TransferToAccountName
        var full = await _repo.GetByIdAsync(created.Id);
        return full!.ToDto();
    }

    public async Task<bool> UpdateAsync(int id, UpdateTransactionRequest request)
    {
        var existing = await _repo.GetByIdAsync(id);
        if (existing is null) return false;

        // repo owns reverse-old / apply-new balance logic
        await _repo.UpdateAsync(request.ToEntity(id));
        return true;
    }

    public Task DeleteAsync(int id) => _repo.DeleteAsync(id);
}