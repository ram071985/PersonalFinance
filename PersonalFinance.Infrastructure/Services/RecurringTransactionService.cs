using Microsoft.Extensions.Logging;
using PersonalFinance.Core.Dtos.Recurring;
using PersonalFinance.Core.Dtos.Transactions;
using PersonalFinance.Core.Entities;
using PersonalFinance.Core.Interfaces;

namespace PersonalFinance.Infrastructure.Services;

public class RecurringTransactionService : IRecurringTransactionService
{
    private readonly IRecurringTransactionRepository _repo;
    private readonly ITransactionService _transactions;
    private readonly ILogger<RecurringTransactionService> _logger;

    public RecurringTransactionService(
        IRecurringTransactionRepository repo,
        ITransactionService transactions,
        ILogger<RecurringTransactionService> logger)
    {
        _repo = repo;
        _transactions = transactions;
        _logger = logger;
    }

    public async Task<IEnumerable<RecurringTransactionDto>> GetAllAsync() =>
        (await _repo.GetAllAsync()).Select(ToDto);

    public async Task<RecurringTransactionDto> CreateAsync(CreateRecurringTransactionRequest request)
    {
        var entity = new RecurringTransaction
        {
            AccountId = request.AccountId,
            CategoryId = request.CategoryId,
            TransferToAccountId = request.TransferToAccountId,
            Amount = request.Amount,
            Type = request.Type,
            Description = request.Description,
            DayOfMonth = request.DayOfMonth,
            StartDate = request.StartDate.Date,
            EndDate = request.EndDate?.Date,
            IsActive = request.IsActive
        };
        var created = await _repo.AddAsync(entity);
        var full = await _repo.GetByIdAsync(created.Id);
        return ToDto(full!);
    }

    public Task<bool> DeleteAsync(int id) => _repo.DeleteAsync(id);

    public async Task<TransactionDto?> GenerateDueAsync(int id)
    {
        var template = await _repo.GetByIdAsync(id);
        if (template is null || !template.IsActive)
            return null;

        var today = DateTime.UtcNow.Date;
        if (!IsDue(template, today))
            return null;

        return await GenerateCoreAsync(template, today);
    }

    public async Task<int> GenerateAllDueAsync(DateTime? asOfUtc = null)
    {
        var day = (asOfUtc ?? DateTime.UtcNow).Date;
        var due = await _repo.GetDueForDateAsync(day);
        var count = 0;

        foreach (var template in due)
        {
            try
            {
                using (CurrentUserService.Impersonate(template.UserId))
                {
                    // Re-load in tenant scope so includes / ownership checks work
                    var local = await _repo.GetByIdAsync(template.Id);
                    if (local is null) continue;
                    if (!IsDue(local, day)) continue;

                    await GenerateCoreAsync(local, day);
                    count++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed generating recurring {RecurringId} for user {UserId}",
                    template.Id, template.UserId);
            }
        }

        return count;
    }

    private async Task<TransactionDto> GenerateCoreAsync(RecurringTransaction template, DateTime day)
    {
        var created = await _transactions.CreateAsync(new CreateTransactionRequest
        {
            AccountId = template.AccountId,
            CategoryId = template.CategoryId,
            TransferToAccountId = template.TransferToAccountId,
            Amount = template.Amount,
            Type = template.Type,
            Description = template.Description,
            Date = day
        });

        template.LastGeneratedDate = day;
        await _repo.UpdateAsync(template);
        return created;
    }

    private static bool IsDue(RecurringTransaction template, DateTime day)
    {
        if (!template.IsActive) return false;
        if (day < template.StartDate.Date) return false;
        if (template.EndDate is not null && day > template.EndDate.Value.Date) return false;

        var dom = template.DayOfMonth;
        if (dom > 28) dom = 28;
        if (day.Day != dom) return false;

        if (template.LastGeneratedDate is not null
            && template.LastGeneratedDate.Value.Year == day.Year
            && template.LastGeneratedDate.Value.Month == day.Month)
            return false;

        return true;
    }

    private static RecurringTransactionDto ToDto(RecurringTransaction r) => new()
    {
        Id = r.Id,
        AccountId = r.AccountId,
        AccountName = r.Account?.Name ?? "",
        CategoryId = r.CategoryId,
        CategoryName = r.Category?.Name,
        TransferToAccountId = r.TransferToAccountId,
        Amount = r.Amount,
        Type = r.Type,
        Description = r.Description,
        DayOfMonth = r.DayOfMonth,
        StartDate = r.StartDate,
        EndDate = r.EndDate,
        LastGeneratedDate = r.LastGeneratedDate,
        IsActive = r.IsActive
    };
}
