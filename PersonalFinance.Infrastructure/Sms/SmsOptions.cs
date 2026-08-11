namespace PersonalFinance.Infrastructure.Sms;

public class SmsOptions
{
    public const string SectionName = "Sms";

    /// <summary>When false, NullSmsSender is registered instead of AzureSmsSender.</summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Azure Communication Services connection string:
    /// endpoint=https://{resource}.communication.azure.com/;accesskey={key}
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>ACS SMS-capable number in E.164, e.g. +15551234567.</summary>
    public string? FromNumber { get; set; }
}