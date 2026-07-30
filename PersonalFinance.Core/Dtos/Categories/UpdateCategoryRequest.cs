using PersonalFinance.Core.Enums;

namespace PersonalFinance.Core.Dtos.Categories;

public class UpdateCategoryRequest
{
    public string Name { get; set; } = string.Empty;
    public CategoryType Type { get; set; }
    public string? Icon { get; set; }
    public string? Color { get; set; }
    public bool IsActive { get; set; } = true;
}