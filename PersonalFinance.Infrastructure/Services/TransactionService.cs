using PersonalFinance.Core.Common;
using PersonalFinance.Core.Dtos.Transactions;
using PersonalFinance.Core.Interfaces;
using PersonalFinance.Core.Mappings;

namespace PersonalFinance.Infrastructure.Services;

public class TransactionService : ITransactionService
{
    private readonly ITransactionRepository _repo;
    private readonly IUnitOfWork _uow;

    public TransactionService(ITransactionRepository repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

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
        var entity = request.ToEntity();

        await _uow.ExecuteInTransactionAsync(async _ =>
        {
            await _repo.AddAsync(entity);
        });

        // Re-fetch with includes for display names
        var full = await _repo.GetByIdAsync(entity.Id);
        return full!.ToDto();
    }

    public async Task<Result> UpdateAsync(int id, UpdateTransactionRequest request)
    {
        var existing = await _repo.GetByIdAsync(id);
        if (existing is null)
            return Result.Fail("Transaction not found.");

        await _uow.ExecuteInTransactionAsync(async _ =>
        {
            await _repo.UpdateAsync(request.ToEntity(id));
        });

        return Result.Ok();
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var deleted = false;

        await _uow.ExecuteInTransactionAsync(async _ =>
        {
            deleted = await _repo.DeleteAsync(id);
        });

        return deleted;
    }
}
