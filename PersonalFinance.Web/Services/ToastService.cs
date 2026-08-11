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
        if (string.IsNullOrWhiteSpace(text)) return;

        var msg = new ToastMessage { Text = text.Trim(), Level = level };
        _messages.Add(msg);
        await NotifyAsync();

        // Auto-dismiss (fire-and-forget on circuit)
        _ = DismissAfterAsync(msg.Id, TimeSpan.FromSeconds(4));
    }

    public Task SuccessAsync(string text) => ShowAsync(text, ToastLevel.Success);
    public Task ErrorAsync(string text) => ShowAsync(text, ToastLevel.Error);
    public Task WarningAsync(string text) => ShowAsync(text, ToastLevel.Warning);
    public Task InfoAsync(string text) => ShowAsync(text, ToastLevel.Info);

    public async Task DismissAsync(Guid id)
    {
        if (_messages.RemoveAll(m => m.Id == id) > 0)
            await NotifyAsync();
    }

    private async Task DismissAfterAsync(Guid id, TimeSpan delay)
    {
        try
        {
            await Task.Delay(delay);
            await DismissAsync(id);
        }
        catch
        {
            // Circuit disposed — ignore
        }
    }

    private async Task NotifyAsync()
    {
        if (OnChange is not null)
            await OnChange.Invoke();
    }
}