using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using PersonalFinance.Core.Dtos.Notifications;
using PersonalFinance.Core.Entities;
using PersonalFinance.Core.Interfaces;
using PersonalFinance.Infrastructure.Identity;

namespace PersonalFinance.Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly INotificationRepository _repo;
    private readonly IEmailSender _email;
    private readonly ISmsSender _sms;
    private readonly UserManager<ApplicationUser> _users;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        INotificationRepository repo,
        IEmailSender email,
        ISmsSender sms,
        UserManager<ApplicationUser> users,
        ICurrentUserService currentUser,
        ILogger<NotificationService> logger)
    {
        _repo = repo;
        _email = email;
        _sms = sms;
        _users = users;
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

        try
        {
            var user = await _users.FindByIdAsync(userId);
            if (user is not null
                && user.SmsNotificationsEnabled
                && !string.IsNullOrWhiteSpace(user.PhoneNumber))
            {
                var phone = NormalizePhone(user.PhoneNumber);
                if (phone is not null)
                {
                    // SMS body keep short
                    var sms = $"Personal Finance: {categoryName} over budget ({spent:0.##}/{limit:0.##}).";
                    await _sms.SendAsync(phone, sms);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send budget SMS for user {UserId}", userId);
        }
    }

    /// <summary>Accepts +1… or 10-digit US numbers; returns E.164 or null.</summary>
    public static string? NormalizePhone(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        var digits = new string(raw.Where(char.IsDigit).ToArray());
        if (raw.TrimStart().StartsWith('+') && digits.Length is >= 10 and <= 15)
            return "+" + digits;
        if (digits.Length == 10)
            return "+1" + digits; // US default
        if (digits.Length == 11 && digits.StartsWith('1'))
            return "+" + digits;
        return null;
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
