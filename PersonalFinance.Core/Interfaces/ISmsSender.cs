namespace PersonalFinance.Core.Interfaces;

public interface ISmsSender
{
    /// <summary>Sends a plain-text SMS. No-op when SMS is disabled/unconfigured.</summary>
    Task SendAsync(string toPhoneE164, string message, CancellationToken ct = default);
}