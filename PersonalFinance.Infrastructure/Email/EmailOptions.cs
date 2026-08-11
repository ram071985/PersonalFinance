namespace PersonalFinance.Infrastructure.Email;

public class EmailOptions
{
    public const string SectionName = "Email";

    /// <summary>None | Smtp | Azure</summary>
    public string Provider { get; set; } = "None";

    public bool Enabled { get; set; }

    // ── SMTP ──────────────────────────────────────────────
    public string Host { get; set; } = "";
    public int Port { get; set; } = 587;
    public bool UseSsl { get; set; } = true;
    public string UserName { get; set; } = "";
    public string Password { get; set; } = "";

    // ── Azure Communication Services Email ────────────────
    /// <summary>endpoint=https://….communication.azure.com/;accesskey=…</summary>
    public string? ConnectionString { get; set; }

    // ── Shared ────────────────────────────────────────────
    /// <summary>Must be a verified sender domain address on ACS (or SMTP from).</summary>
    public string FromAddress { get; set; } = "noreply@personalfinance.local";
    public string FromName { get; set; } = "Personal Finance";
}