using PersonalFinance.Core.Enums;

namespace PersonalFinance.Core.Entities;

public class Account
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public AccountType Type { get; set; }
    public decimal Balance { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string? Institution { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// SQL Server rowversion for optimistic concurrency on balance updates.
    /// </summary>
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();

    // ── Domain: balance effects ──────────────────────────────

    public void ApplyIncome(decimal amount)
    {
        EnsurePositive(amount);
        Balance += amount;
        Touch();
    }

    public void ReverseIncome(decimal amount)
    {
        EnsurePositive(amount);
        Balance -= amount;
        Touch();
    }

    public void ApplyExpense(decimal amount)
    {
        EnsurePositive(amount);
        Balance -= amount;
        Touch();
    }

    public void ReverseExpense(decimal amount)
    {
        EnsurePositive(amount);
        Balance += amount;
        Touch();
    }

    public void ApplyTransferOut(decimal amount)
    {
        EnsurePositive(amount);
        Balance -= amount;
        Touch();
    }

    public void ReverseTransferOut(decimal amount)
    {
        EnsurePositive(amount);
        Balance += amount;
        Touch();
    }

    public void ApplyTransferIn(decimal amount)
    {
        EnsurePositive(amount);
        Balance += amount;
        Touch();
    }

    public void ReverseTransferIn(decimal amount)
    {
        EnsurePositive(amount);
        Balance -= amount;
        Touch();
    }

    /// <summary>
    /// Applies or reverses the balance effect of a transaction on this account
    /// when this account is the primary (source) account.
    /// </summary>
    public void ApplyPrimaryEffect(TransactionType type, decimal amount, bool reverse)
    {
        switch (type)
        {
            case TransactionType.Income:
                if (reverse) ReverseIncome(amount); else ApplyIncome(amount);
                break;
            case TransactionType.Expense:
                if (reverse) ReverseExpense(amount); else ApplyExpense(amount);
                break;
            case TransactionType.Transfer:
                if (reverse) ReverseTransferOut(amount); else ApplyTransferOut(amount);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown transaction type.");
        }
    }

    private static void EnsurePositive(decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be greater than zero.");
    }

    private void Touch() => UpdatedAt = DateTime.UtcNow;
}
