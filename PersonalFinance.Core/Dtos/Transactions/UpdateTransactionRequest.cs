using PersonalFinance.Core.Enums;

namespace PersonalFinance.Core.Dtos.Transactions;

public class UpdateTransactionRequest
{
    public int AccountId { get; set; }
    public int? CategoryId { get; set; }
    public int? TransferToAccountId { get; set; }
    public decimal Amount { get; set; }
    public TransactionType Type { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTime Date { get; set; }
}