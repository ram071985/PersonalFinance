using PersonalFinance.Core.Interfaces;
using PersonalFinance.Infrastructure.Data;
using PersonalFinance.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace PersonalFinance.Api.Workers;

/// <summary>
/// Hourly check; generates due recurring transactions for all tenants.
/// </summary>
public sealed class RecurringGenerationWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RecurringGenerationWorker> _logger;

    public RecurringGenerationWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<RecurringGenerationWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try { await Task.Delay(TimeSpan.FromSeconds(20), stoppingToken); }
        catch (OperationCanceledException) { return; }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunOnceAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Recurring generation cycle failed");
            }

            try { await Task.Delay(TimeSpan.FromHours(1), stoppingToken); }
            catch (OperationCanceledException) { break; }
        }
    }

    private async Task RunOnceAsync()
    {
        // Discover due templates system-wide (ignore filters)
        List<(int Id, string UserId)> due;
        using (var scope = _scopeFactory.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var today = DateTime.UtcNow.Date;
            var day = Math.Min(today.Day, 28);

            due = await db.RecurringTransactions
                .IgnoreQueryFilters()
                .Where(r => r.IsActive
                            && r.DayOfMonth == day
                            && r.StartDate <= today
                            && (r.EndDate == null || r.EndDate >= today)
                            && (r.LastGeneratedDate == null
                                || r.LastGeneratedDate.Value.Year != today.Year
                                || r.LastGeneratedDate.Value.Month != today.Month))
                .Select(r => new ValueTuple<int, string>(r.Id, r.UserId))
                .ToListAsync();
        }

        if (due.Count == 0)
        {
            _logger.LogDebug("No recurring transactions due");
            return;
        }

        var generated = 0;
        foreach (var (id, userId) in due)
        {
            try
            {
                using (CurrentUserService.Impersonate(userId))
                using (var scope = _scopeFactory.CreateScope())
                {
                    var service = scope.ServiceProvider.GetRequiredService<IRecurringTransactionService>();
                    var tx = await service.GenerateDueAsync(id);
                    if (tx is not null) generated++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed recurring {Id} for user {UserId}", id, userId);
            }
        }

        _logger.LogInformation("Generated {Count} recurring transaction(s)", generated);
    }
}
