namespace PersonalFinance.Web.Services;

public enum ToastLevel
{
    Info,
    Success,
    Warning,
    Error
}

public sealed class ToastMessage
{
    public Guid Id { get; } = Guid.NewGuid();
    public string Text { get; init; } = "";
    public ToastLevel Level { get; init; } = ToastLevel.Info;
}

/// <summary>
/// Circuit-scoped toast notifications for success / validation / network errors.
/// </summary>
public class ToastService
{
    public event Func<Task>? OnChange;

    private readonly List<ToastMessage> _messages = new();
    public IReadOnlyList<ToastMessage> Messages => _messages;

    public async Task ShowAsync(string text, ToastLevel level = ToastLevel.Info)
    {
        _messages.Add(new ToastMessage { Text = text, Level = level });
        if (OnChange is not null) await OnChange.Invoke();

        // Auto-dismiss after a few seconds is handled by the UI component.
    }

    public Task SuccessAsync(string text) => ShowAsync(text, ToastLevel.Success);
    public Task ErrorAsync(string text) => ShowAsync(text, ToastLevel.Error);
    public Task WarningAsync(string text) => ShowAsync(text, ToastLevel.Warning);

    public async Task DismissAsync(Guid id)
    {
        _messages.RemoveAll(m => m.Id == id);
        if (OnChange is not null) await OnChange.Invoke();
    }
}