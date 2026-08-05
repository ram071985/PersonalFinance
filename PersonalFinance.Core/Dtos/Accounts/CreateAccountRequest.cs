using System.ComponentModel.DataAnnotations;
using PersonalFinance.Core.Enums;

namespace PersonalFinance.Core.Dtos.Accounts;

public class CreateAccountRequest
{
    [Required(ErrorMessage = "Account name is required.")]
    [MaxLength(100, ErrorMessage = "Account name must be 100 characters or fewer.")]
    public string Name { get; set; } = string.Empty;

    [EnumDataType(typeof(AccountType), ErrorMessage = "Invalid account type.")]
    public AccountType Type { get; set; } = AccountType.Checking;

    [Range(0, double.MaxValue, ErrorMessage = "Opening balance cannot be negative.")]
    public decimal Balance { get; set; }

    [MaxLength(100, ErrorMessage = "Institution must be 100 characters or fewer.")]
    public string? Institution { get; set; }

    [MaxLength(500, ErrorMessage = "Notes must be 500 characters or fewer.")]
    public string? Notes { get; set; }

    public bool IsActive { get; set; } = true;
}