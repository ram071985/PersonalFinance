using Microsoft.Extensions.Logging;
using PersonalFinance.Core.Common;
using PersonalFinance.Core.Dtos.Transactions;
using PersonalFinance.Core.Entities;
using PersonalFinance.Core.Enums;
using PersonalFinance.Core.Interfaces;
using PersonalFinance.Core.Mappings;

namespace PersonalFinance.Infrastructure.Services;

public class TransactionService : ITransactionService
{
    private readonly ITransactionRepository _repo;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;
    private readonly IBudgetService _budgets;
    private readonly INotificationService _notifications;
    private readonly ILogger<TransactionService> _logger;

    public TransactionService(
        ITransactionRepository repo,
        IUnitOfWork uow,
        ICurrentUserService currentUser,
        IBudgetService budgets,
        INotificationService notifications,
        ILogger<TransactionService> logger)
    {
        _repo = repo;
        _uow = uow;
        _currentUser = currentUser;
        _budgets = budgets;
        _notifications = notifications;
        _logger = logger;
    }

    public async Task<IEnumerable<TransactionDto>> GetAllAsync() =>
        (await _repo.GetAllAsync()).ToDtoList();

    public async Task<PagedResult<TransactionDto>> GetPagedAsync(int page = 1, int pageSize = 20)
    {
        var (items, total) = await _repo.GetPagedAsync(page, pageSize);
        return new PagedResult<TransactionDto>
        {
            Items = items.ToDtoList(),
            TotalCount = total,
            Page = Math.Max(1, page),
            PageSize = Math.Clamp(pageSize, 1, 100)
        };
    }

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
        Transaction created = null!;

        await _uow.ExecuteInTransactionAsync(async _ =>
        {
            created = await _repo.AddAsync(entity);
        });

        _logger.LogInformation(
            "Transaction created. UserId={UserId} TransactionId={TransactionId} AccountId={AccountId} Type={Type} Amount={Amount}",
            _currentUser.UserId,
            created.Id,
            created.AccountId,
            created.Type,
            created.Amount);

        var full = await _repo.GetByIdAsync(created.Id);
        var dto = (full ?? created).ToDto();

        await TryNotifyBudgetAsync(dto);
        return dto;
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

        _logger.LogInformation(
            "Transaction updated. UserId={UserId} TransactionId={TransactionId} AccountId={AccountId} Type={Type} Amount={Amount}",
            _currentUser.UserId,
            id,
            request.AccountId,
            request.Type,
            request.Amount);

        return Result.Ok();
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var existing = await _repo.GetByIdAsync(id);
        if (existing is null) return false;

        await _uow.ExecuteInTransactionAsync(async _ =>
        {
            await _repo.DeleteAsync(id);
        });

        return true;
    }

    private async Task TryNotifyBudgetAsync(TransactionDto tx)
    {
        try
        {
            if (tx.Type != TransactionType.Expense || tx.CategoryId is null)
                return;
            if (_currentUser.UserId is null)
                return;

            var budgets = await _budgets.GetByMonthAsync(tx.Date.Year, tx.Date.Month);
            var match = budgets.FirstOrDefault(b => b.CategoryId == tx.CategoryId.Value && b.IsOverBudget);
            if (match is null)
                return;

            await _notifications.NotifyBudgetExceededAsync(
                _currentUser.UserId,
                _currentUser.Email ?? "",
                match.CategoryName,
                match.Spent,
                match.Amount);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Budget notification failed for transaction {Id}", tx.Id);
        }
    }
}
