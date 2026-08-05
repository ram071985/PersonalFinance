using PersonalFinance.Core.Entities;

namespace PersonalFinance.Core.Interfaces;

public interface INotificationRepository
{
    Task<IReadOnlyList<Notification>> GetRecentAsync(int take = 20);
    Task<int> CountUnreadAsync();
    Task AddAsync(Notification notification);
    Task MarkReadAsync(int id, string userId);
    Task MarkAllReadAsync(string userId);
}