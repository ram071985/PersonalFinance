namespace PersonalFinance.Core.Dtos.Notifications;

public class NotificationDto
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Message { get; set; } = "";
    public string Kind { get; set; } = "info";
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class SmsSettingsDto
{
    public string? PhoneNumber { get; set; }
    public bool SmsEnabled { get; set; }
}