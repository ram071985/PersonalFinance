using System.ComponentModel.DataAnnotations;
using PersonalFinance.Core.Enums;

namespace PersonalFinance.Models;

public class CategoryFormModel
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Name is required")]
    [StringLength(80)]
    public string Name { get; set; } = string.Empty;

    public CategoryType Type { get; set; } = CategoryType.Expense;

    [StringLength(10)]
    public string? Icon { get; set; }

    [StringLength(20)]
    public string? Color { get; set; }

    public bool IsActive { get; set; } = true;

    public bool IsSaving { get; set; }
    public string? ErrorMessage { get; set; }
}