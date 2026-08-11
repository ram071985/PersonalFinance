using PersonalFinance.Core.Enums;

namespace PersonalFinance.Core.Entities;

public class Transaction
{
    public int Id { get; set; }
    public int AccountId { get; set; }
    public int? CategoryId { get; set; }
    public int? TransferToAccountId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public TransactionType Type { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTime Date { get; set; } = DateTime.UtcNow.Date;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; }
    /// <summary>Plaid transaction_id for dedupe.</summary>
    public string? ExternalId { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Account Account { get; set; } = null!;
    public Category? Category { get; set; }
    public Account? TransferToAccount { get; set; }
}