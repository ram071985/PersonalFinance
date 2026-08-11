namespace PersonalFinance.Infrastructure.Plaid;

public class PlaidOptions
{
    public const string SectionName = "Plaid";

    public bool Enabled { get; set; }
    /// <summary>sandbox | development | production</summary>
    public string Env { get; set; } = "sandbox";
    public string ClientId { get; set; } = "";
    public string Secret { get; set; } = "";
    /// <summary>Optional shared secret for webhook URL ?key=</summary>
    public string? WebhookSecret { get; set; }
    /// <summary>Public HTTPS webhook URL registered with Plaid (optional).</summary>
    public string? WebhookUrl { get; set; }

    public string BaseUrl => Env.ToLowerInvariant() switch
    {
        "production" => "https://production.plaid.com",
        "development" => "https://development.plaid.com",
        _ => "https://sandbox.plaid.com"
    };
}