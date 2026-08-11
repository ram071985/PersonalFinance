using System.ComponentModel.DataAnnotations;
using PersonalFinance.Core.Enums;

namespace PersonalFinance.Core.Dtos.Categories;

public class UpdateCategoryRequest
{
    [Required(ErrorMessage = "Category name is required.")]
    [MaxLength(50, ErrorMessage = "Category name must be 50 characters or fewer.")]
    public string Name { get; set; } = string.Empty;

    [EnumDataType(typeof(CategoryType), ErrorMessage = "Invalid category type.")]
    public CategoryType Type { get; set; }

    [MaxLength(50, ErrorMessage = "Icon must be 50 characters or fewer.")]
    public string? Icon { get; set; }

    [MaxLength(20, ErrorMessage = "Color must be 20 characters or fewer.")]
    public string? Color { get; set; }

    public bool IsActive { get; set; } = true;
}