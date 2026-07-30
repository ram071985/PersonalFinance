using PersonalFinance.Core.Enums;

namespace PersonalFinance.Core.Dtos.Categories;

public class CreateCategoryRequest
{
    public string Name { get; set; } = string.Empty;
    public CategoryType Type { get; set; } = CategoryType.Expense;
    public string? Icon { get; set; }
    public string? Color { get; set; }
    public bool IsActive { get; set; } = true;
}