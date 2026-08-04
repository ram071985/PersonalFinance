using PersonalFinance.Core.Dtos.Transactions;

namespace PersonalFinance.Core.Interfaces;

public interface INotificationService
{
    Task<IReadOnlyList<NotificationDto>> GetRecentAsync(int take = 20);
    Task<int> GetUnreadCountAsync();
    Task MarkReadAsync(int id);
    Task MarkAllReadAsync();
    /// <summary>In-app + optional email when a category budget is exceeded.</summary>
    Task NotifyBudgetExceededAsync(string userId, string userEmail, string categoryName, decimal spent, decimal limit);
}