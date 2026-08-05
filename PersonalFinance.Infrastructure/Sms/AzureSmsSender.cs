using Azure.Communication.Sms;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PersonalFinance.Core.Interfaces;

namespace PersonalFinance.Infrastructure.Sms;

/// <summary>Sends SMS via Azure Communication Services.</summary>
public class AzureSmsSender : ISmsSender
{
    private readonly SmsOptions _options;
    private readonly ILogger<AzureSmsSender> _logger;
    private readonly SmsClient? _client;

    public AzureSmsSender(IOptions<SmsOptions> options, ILogger<AzureSmsSender> logger)
    {
        _options = options.Value;
        _logger = logger;

        if (_options.Enabled && !string.IsNullOrWhiteSpace(_options.ConnectionString))
            _client = new SmsClient(_options.ConnectionString);
    }

    public async Task SendAsync(string toPhoneE164, string message, CancellationToken ct = default)
    {
        if (_client is null || string.IsNullOrWhiteSpace(_options.FromNumber))
        {
            _logger.LogDebug("Azure SMS not configured; skip to {Phone}", toPhoneE164);
            return;
        }

        try
        {
            var response = await _client.SendAsync(
                from: _options.FromNumber,
                to: toPhoneE164,
                message: message,
                cancellationToken: ct);

            var result = response.Value;
            _logger.LogInformation(
                "Azure SMS sent to {Phone}. MessageId={MessageId} Successful={Ok}",
                toPhoneE164,
                result.MessageId,
                result.Successful);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Azure SMS failed to {Phone}", toPhoneE164);
            throw;
        }
    }
}