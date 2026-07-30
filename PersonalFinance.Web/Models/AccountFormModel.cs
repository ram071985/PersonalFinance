using System.ComponentModel.DataAnnotations;
using PersonalFinance.Core.Enums;

namespace PersonalFinance.Web.Models;

public class AccountFormModel
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Name is required")]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    public AccountType Type { get; set; } = AccountType.Checking;

    public decimal Balance { get; set; }

    [StringLength(100)]
    public string? Institution { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }

    public bool IsActive { get; set; } = true;

    public bool IsSaving { get; set; }
    public string? ErrorMessage { get; set; }
}