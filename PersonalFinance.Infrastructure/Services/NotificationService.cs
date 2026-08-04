using Microsoft.Extensions.Logging;
using PersonalFinance.Core.Dtos.Transactions;
using PersonalFinance.Core.Entities;
using PersonalFinance.Core.Interfaces;

namespace PersonalFinance.Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly INotificationRepository _repo;
    private readonly IEmailSender _email;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        INotificationRepository repo,
        IEmailSender email,
        ICurrentUserService currentUser,
        ILogger<NotificationService> logger)
    {
        _repo = repo;
        _email = email;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<IReadOnlyList<NotificationDto>> GetRecentAsync(int take = 20) =>
        (await _repo.GetRecentAsync(take)).Select(ToDto).ToList();

    public Task<int> GetUnreadCountAsync() => _repo.CountUnreadAsync();

    public async Task MarkReadAsync(int id)
    {
        if (_currentUser.UserId is null) return;
        await _repo.MarkReadAsync(id, _currentUser.UserId);
    }

    public async Task MarkAllReadAsync()
    {
        if (_currentUser.UserId is null) return;
        await _repo.MarkAllReadAsync(_currentUser.UserId);
    }

    public async Task NotifyBudgetExceededAsync(
        string userId,
        string userEmail,
        string categoryName,
        decimal spent,
        decimal limit)
    {
        var title = "Budget exceeded";
        var message = $"{categoryName}: spent {spent:C} of {limit:C} this month.";

        await _repo.AddAsync(new Notification
        {
            UserId = userId,
            Title = title,
            Message = message,
            Kind = "budget",
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        });

        try
        {
            if (!string.IsNullOrWhiteSpace(userEmail))
            {
                await _email.SendAsync(
                    userEmail,
                    $"[Personal Finance] {title}: {categoryName}",
                    $"<p><strong>{title}</strong></p><p>{message}</p><p>Log in to review your budgets.</p>");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send budget email to {Email}", userEmail);
        }
    }

    private static NotificationDto ToDto(Notification n) => new()
    {
        Id = n.Id,
        Title = n.Title,
        Message = n.Message,
        Kind = n.Kind,
        IsRead = n.IsRead,
        CreatedAt = n.CreatedAt
    };
}
