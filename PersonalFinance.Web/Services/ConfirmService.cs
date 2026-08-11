namespace PersonalFinance.Web.Services;

/// <summary>Circuit-scoped modal confirmations (delete, disconnect, etc.).</summary>
public class ConfirmService
{
    private TaskCompletionSource<bool>? _tcs;

    public bool IsOpen { get; private set; }
    public string Title { get; private set; } = "Confirm";
    public string Message { get; private set; } = "Are you sure?";
    public string ConfirmText { get; private set; } = "Confirm";
    public string CancelText { get; private set; } = "Cancel";
    public bool IsDanger { get; private set; } = true;

    public event Func<Task>? OnChange;

    public async Task<bool> ShowAsync(
        string message,
        string title = "Confirm",
        string confirmText = "Confirm",
        string cancelText = "Cancel",
        bool danger = true)
    {
        // If a dialog is already open, cancel the previous waiter
        _tcs?.TrySetResult(false);

        Title = title;
        Message = message;
        ConfirmText = confirmText;
        CancelText = cancelText;
        IsDanger = danger;
        IsOpen = true;
        _tcs = new TaskCompletionSource<bool>();
        await NotifyAsync();
        return await _tcs.Task;
    }

    public async Task AcceptAsync()
    {
        IsOpen = false;
        _tcs?.TrySetResult(true);
        _tcs = null;
        await NotifyAsync();
    }

    public async Task CancelAsync()
    {
        IsOpen = false;
        _tcs?.TrySetResult(false);
        _tcs = null;
        await NotifyAsync();
    }

    private async Task NotifyAsync()
    {
        if (OnChange is not null) await OnChange.Invoke();
    }
}