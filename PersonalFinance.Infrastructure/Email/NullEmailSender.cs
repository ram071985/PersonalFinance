using Microsoft.Extensions.Logging;
using PersonalFinance.Core.Interfaces;

namespace PersonalFinance.Infrastructure.Email;

public class NullEmailSender : IEmailSender
{
    private readonly ILogger<NullEmailSender> _logger;
    public NullEmailSender(ILogger<NullEmailSender> logger) => _logger = logger;

    public Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken ct = default)
    {
        _logger.LogDebug("NullEmailSender: {To} / {Subject}", toEmail, subject);
        return Task.CompletedTask;
    }
}