namespace PersonalFinance.Core.Entities;

/// <summary>
/// One Plaid Item = one user↔institution connection. Access token is stored encrypted.
/// User does not re-auth via Plaid on each app login.
/// </summary>
public class PlaidItem
{
    public int Id { get; set; }
    public string UserId { get; set; } = "";
    /// <summary>Plaid item_id</summary>
    public string ItemId { get; set; } = "";
    /// <summary>Data-Protection protected access_token (never log).</summary>
    public string AccessTokenProtected { get; set; } = "";
    public string? InstitutionId { get; set; }
    public string? InstitutionName { get; set; }
    /// <summary>Cursor for /transactions/sync</summary>
    public string? SyncCursor { get; set; }
    public string Status { get; set; } = "active";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastSyncedAt { get; set; }
    public string? LastError { get; set; }
}