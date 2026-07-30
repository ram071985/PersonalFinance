using PersonalFinance.Core.Enums;
namespace PersonalFinance.Core.Dtos.Transactions;

public class TransactionDto
{
    public int Id { get; set; }
    public int AccountId { get; set; }
    public string AccountName { get; set; } = string.Empty;
    public int? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public string? CategoryIcon { get; set; }
    public int? TransferToAccountId { get; set; }
    public string? TransferToAccountName { get; set; }
    public decimal Amount { get; set; }
    public TransactionType Type { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTime Date { get; set; }
    public DateTime CreatedAt { get; set; }
}