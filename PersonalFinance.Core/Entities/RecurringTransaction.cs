using PersonalFinance.Core.Enums;

namespace PersonalFinance.Core.Entities;

/// <summary>
/// Template for transactions that repeat on a schedule (monthly day-of-month).
/// Generation of concrete Transaction rows is explicit (API endpoint), not automatic.
/// </summary>
public class RecurringTransaction
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;

    public int AccountId { get; set; }
    public Account? Account { get; set; }

    public int? CategoryId { get; set; }
    public Category? Category { get; set; }

    public int? TransferToAccountId { get; set; }
    public Account? TransferToAccount { get; set; }

    public decimal Amount { get; set; }
    public TransactionType Type { get; set; }
    public string Description { get; set; } = string.Empty;

    /// <summary>Day of month to generate (1–28 recommended).</summary>
    public int DayOfMonth { get; set; } = 1;

    public DateTime StartDate { get; set; } = DateTime.UtcNow.Date;
    public DateTime? EndDate { get; set; }
    public DateTime? LastGeneratedDate { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}