namespace PersonalFinance.Core.Entities;

public class Budget
{
    public int Id { get; set; }
    public int CategoryId { get; set; }
    public decimal Amount { get; set; }
    public int Year { get; set; }
    public string UserId { get; set; } = string.Empty;
    public int Month { get; set; } // 1-12
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    public Category Category { get; set; } = null!;
}