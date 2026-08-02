using System.ComponentModel.DataAnnotations;
using PersonalFinance.Core.Enums;

namespace PersonalFinance.Core.Dtos.Recurring;

public class RecurringTransactionDto
{
    public int Id { get; set; }
    public int AccountId { get; set; }
    public string AccountName { get; set; } = "";
    public int? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public int? TransferToAccountId { get; set; }
    public decimal Amount { get; set; }
    public TransactionType Type { get; set; }
    public string Description { get; set; } = "";
    public int DayOfMonth { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime? LastGeneratedDate { get; set; }
    public bool IsActive { get; set; }
}

public class CreateRecurringTransactionRequest
{
    [Range(1, int.MaxValue)]
    public int AccountId { get; set; }

    public int? CategoryId { get; set; }
    public int? TransferToAccountId { get; set; }

    [Range(0.01, double.MaxValue)]
    public decimal Amount { get; set; }

    [Required]
    public TransactionType Type { get; set; }

    [Required, MaxLength(200)]
    public string Description { get; set; } = "";

    [Range(1, 28)]
    public int DayOfMonth { get; set; } = 1;

    public DateTime StartDate { get; set; } = DateTime.UtcNow.Date;
    public DateTime? EndDate { get; set; }
    public bool IsActive { get; set; } = true;
}