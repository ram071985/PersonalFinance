using Moq;
using PersonalFinance.Core.Common;
using PersonalFinance.Core.Dtos.Categories;
using PersonalFinance.Core.Entities;
using PersonalFinance.Core.Enums;
using PersonalFinance.Core.Interfaces;
using PersonalFinance.Infrastructure.Services;

namespace PersonalFinance.Tests.Services;

[TestFixture]
public class CategoryServiceTests
{
    private Mock<ICategoryRepository> _repo = null!;
    private CategoryService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _repo = new Mock<ICategoryRepository>();
        _sut = new CategoryService(_repo.Object);
    }

    [Test]
    public async Task GetAllAsync_ReturnsMappedDtos()
    {
        var categories = new List<Category>
        {
            new() { Id = 1, Name = "Salary", Type = CategoryType.Income, Icon = "💰", IsActive = true, CreatedAt = DateTime.UtcNow },
            new() { Id = 2, Name = "Groceries", Type = CategoryType.Expense, Icon = "🛒", IsActive = true, CreatedAt = DateTime.UtcNow }
        };

        _repo.Setup(r => r.GetAllAsync()).ReturnsAsync(categories);

        var result = (await _sut.GetAllAsync()).ToList();

        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(result[0].Name, Is.EqualTo("Salary"));
        Assert.That(result[0].Type, Is.EqualTo(CategoryType.Income));
        Assert.That(result[1].Name, Is.EqualTo("Groceries"));
    }

    [Test]
    public async Task GetByTypeAsync_FiltersCorrectly()
    {
        var expenseCats = new List<Category>
        {
            new() { Id = 2, Name = "Food", Type = CategoryType.Expense, IsActive = true, CreatedAt = DateTime.UtcNow }
        };

        _repo.Setup(r => r.GetByTypeAsync(CategoryType.Expense)).ReturnsAsync(expenseCats);

        var result = (await _sut.GetByTypeAsync(CategoryType.Expense)).ToList();

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].Type, Is.EqualTo(CategoryType.Expense));
        _repo.Verify(r => r.GetByTypeAsync(CategoryType.Expense), Times.Once);
    }

    [Test]
    public async Task GetByIdAsync_WhenFound_ReturnsDto()
    {
        var category = new Category
        {
            Id = 3,
            Name = "Rent",
            Type = CategoryType.Expense,
            Icon = "🏠",
            Color = "#ff0000",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _repo.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(category);

        var result = await _sut.GetByIdAsync(3);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Name, Is.EqualTo("Rent"));
        Assert.That(result.Icon, Is.EqualTo("🏠"));
        Assert.That(result.Color, Is.EqualTo("#ff0000"));
    }

    [Test]
    public async Task GetByIdAsync_WhenNotFound_ReturnsNull()
    {
        _repo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Category?)null);

        var result = await _sut.GetByIdAsync(99);

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task CreateAsync_MapsAndReturnsDto()
    {
        var request = new CreateCategoryRequest
        {
            Name = "Utilities",
            Type = CategoryType.Expense,
            Icon = "⚡",
            Color = "#00ff00",
            IsActive = true
        };

        var created = new Category
        {
            Id = 7,
            Name = request.Name,
            Type = request.Type,
            Icon = request.Icon,
            Color = request.Color,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _repo.Setup(r => r.AddAsync(It.IsAny<Category>())).ReturnsAsync(created);

        var result = await _sut.CreateAsync(request);

        Assert.That(result.Id, Is.EqualTo(7));
        Assert.That(result.Name, Is.EqualTo("Utilities"));
        _repo.Verify(r => r.AddAsync(It.Is<Category>(c =>
            c.Name == "Utilities" && c.Type == CategoryType.Expense)), Times.Once);
    }

    [Test]
    public async Task UpdateAsync_WhenExists_UpdatesAndReturnsTrue()
    {
        var existing = new Category
        {
            Id = 1,
            Name = "Old",
            Type = CategoryType.Expense,
            IsActive = true
        };

        var request = new UpdateCategoryRequest
        {
            Name = "Updated Category",
            Type = CategoryType.Income,
            Icon = "📈",
            Color = "#0000ff",
            IsActive = false
        };

        _repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existing);
        _repo.Setup(r => r.UpdateAsync(It.IsAny<Category>())).Returns(Task.CompletedTask);

        var result = await _sut.UpdateAsync(1, request);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(existing.Name, Is.EqualTo("Updated Category"));
        Assert.That(existing.Type, Is.EqualTo(CategoryType.Income));
        Assert.That(existing.IsActive, Is.False);
        _repo.Verify(r => r.UpdateAsync(existing), Times.Once);
    }

    [Test]
    public async Task UpdateAsync_WhenNotFound_ReturnsFalse()
    {
        _repo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Category?)null);

        var result = await _sut.UpdateAsync(99, new UpdateCategoryRequest { Name = "X" });

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error, Is.EqualTo("Category not found."));
        _repo.Verify(r => r.UpdateAsync(It.IsAny<Category>()), Times.Never);
    }

    [Test]
    public async Task DeleteAsync_CallsRepository()
    {
        _repo.Setup(r => r.DeleteAsync(4)).ReturnsAsync(true);

        var deleted = await _sut.DeleteAsync(4);

        Assert.That(deleted, Is.True);
        _repo.Verify(r => r.DeleteAsync(4), Times.Once);
    }
}
