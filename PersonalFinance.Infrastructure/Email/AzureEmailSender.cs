using Azure;
using Azure.Communication.Email;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PersonalFinance.Core.Interfaces;

namespace PersonalFinance.Infrastructure.Email;

/// <summary>Azure Communication Services Email implementation of IEmailSender.</summary>
public class AzureEmailSender : IEmailSender
{
    private readonly EmailOptions _options;
    private readonly ILogger<AzureEmailSender> _logger;
    private readonly EmailClient? _client;

    public AzureEmailSender(IOptions<EmailOptions> options, ILogger<AzureEmailSender> logger)
    {
        _options = options.Value;
        _logger = logger;

        if (_options.Enabled
            && !string.IsNullOrWhiteSpace(_options.ConnectionString))
        {
            _client = new EmailClient(_options.ConnectionString);
        }
    }

    public async Task SendAsync(
        string toEmail,
        string subject,
        string htmlBody,
        CancellationToken ct = default)
    {
        if (_client is null)
        {
            _logger.LogInformation(
                "Azure email not configured — skip {To}: {Subject}", toEmail, subject);
            return;
        }

        if (string.IsNullOrWhiteSpace(_options.FromAddress))
            throw new InvalidOperationException(
                "Email:FromAddress is required when using Azure Communication Services Email.");

        var content = new EmailContent(subject)
        {
            Html = htmlBody,
            PlainText = StripTags(htmlBody)
        };

        var recipients = new EmailRecipients(
            new List<EmailAddress> { new(toEmail) });

        var message = new EmailMessage(
            senderAddress: _options.FromAddress,
            content: content,
            recipients: recipients);

        try
        {
            EmailSendOperation operation =
                await _client.SendAsync(WaitUntil.Started, message, ct);

            _logger.LogInformation(
                "Azure email queued to {To}: {Subject}. OperationId={OperationId}",
                toEmail,
                subject,
                operation.Id);
        }
        catch (RequestFailedException ex)
        {
            _logger.LogWarning(
                ex,
                "Azure email failed to {To}: {Subject}. Status={Status} Error={Error}",
                toEmail,
                subject,
                ex.Status,
                ex.ErrorCode);
            throw;
        }
    }

    private static string StripTags(string html)
    {
        if (string.IsNullOrEmpty(html)) return string.Empty;
        var buffer = new char[html.Length];
        var n = 0;
        var inside = false;
        foreach (var c in html)
        {
            if (c == '<') { inside = true; continue; }
            if (c == '>') { inside = false; continue; }
            if (!inside) buffer[n++] = c;
        }
        return new string(buffer, 0, n).Trim();
    }
}
