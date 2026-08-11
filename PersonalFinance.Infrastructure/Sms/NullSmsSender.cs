using Microsoft.Extensions.Logging;
using PersonalFinance.Core.Interfaces;

namespace PersonalFinance.Infrastructure.Sms;

public class NullSmsSender : ISmsSender
{
    private readonly ILogger<NullSmsSender> _logger;

    public NullSmsSender(ILogger<NullSmsSender> logger) => _logger = logger;

    public Task SendAsync(string toPhoneE164, string message, CancellationToken ct = default)
    {
        _logger.LogInformation("SMS skipped → {Phone}: {Message}", toPhoneE164, message);
        return Task.CompletedTask;
    }
}