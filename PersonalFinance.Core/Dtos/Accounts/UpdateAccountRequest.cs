using PersonalFinance.Core.Enums;

namespace PersonalFinance.Core.Dtos.Accounts;

public class UpdateAccountRequest
{
    public string Name { get; set; } = string.Empty;
    public AccountType Type { get; set; }
    public decimal Balance { get; set; }
    public string? Institution { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
}