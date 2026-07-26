using PersonalFinance.Core.Entities;
using PersonalFinance.Core.Interfaces;

namespace PersonalFinance.Infrastructure.Services;

public class TransactionService : ITransactionService
{
    private readonly ITransactionRepository _repo;

    public TransactionService(ITransactionRepository repo) => _repo = repo;

    public Task<IEnumerable<Transaction>> GetAllAsync() => _repo.GetAllAsync();

    public Task<IEnumerable<Transaction>> GetRecentAsync(int count = 10) => _repo.GetRecentAsync(count);

    public Task<IEnumerable<Transaction>> GetByAccountIdAsync(int accountId) => _repo.GetByAccountIdAsync(accountId);

    public Task<Transaction?> GetByIdAsync(int id) => _repo.GetByIdAsync(id);

    public Task<Transaction> CreateAsync(Transaction transaction) => _repo.AddAsync(transaction);

    public async Task<bool> UpdateAsync(int id, Transaction input)
    {
        var existing = await _repo.GetByIdAsync(id);
        if (existing is null) return false;

        input.Id = id;
        await _repo.UpdateAsync(input);
        return true;
    }

    public Task DeleteAsync(int id) => _repo.DeleteAsync(id);
}