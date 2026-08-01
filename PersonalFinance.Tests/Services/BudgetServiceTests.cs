using Moq;
using PersonalFinance.Core.Common;
using PersonalFinance.Core.Dtos.Budgets;
using PersonalFinance.Core.Entities;
using PersonalFinance.Core.Interfaces;
using PersonalFinance.Infrastructure.Services;

namespace PersonalFinance.Tests.Services;

[TestFixture]
public class BudgetServiceTests
{
    private Mock<IBudgetRepository> _repo = null!;
    private BudgetService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _repo = new Mock<IBudgetRepository>();
        _sut = new BudgetService(_repo.Object);
    }

    private static Budget CreateSampleBudget(int id = 1) => new()
    {
        Id = id,
        CategoryId = 5,
        Amount = 300m,
        Year = 2026,
        Month = 8,
        Notes = "Monthly food",
        CreatedAt = DateTime.UtcNow,
        Category = new Category { Id = 5, Name = "Groceries", Icon = "🛒" }
    };

    [Test]
    public async Task GetAllAsync_ReturnsMappedDtos()
    {
        var budgets = new List<Budget> { CreateSampleBudget(1), CreateSampleBudget(2) };
        _repo.Setup(r => r.GetAllAsync()).ReturnsAsync(budgets);

        var result = (await _sut.GetAllAsync()).ToList();

        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(result[0].CategoryName, Is.EqualTo("Groceries"));
        Assert.That(result[0].Amount, Is.EqualTo(300m));
    }

    [Test]
    public async Task GetByMonthAsync_PassesYearAndMonth()
    {
        var budgets = new List<Budget> { CreateSampleBudget() };
        _repo.Setup(r => r.GetByMonthAsync(2026, 8)).ReturnsAsync(budgets);

        var result = (await _sut.GetByMonthAsync(2026, 8)).ToList();

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].Year, Is.EqualTo(2026));
        Assert.That(result[0].Month, Is.EqualTo(8));
        _repo.Verify(r => r.GetByMonthAsync(2026, 8), Times.Once);
    }

    [Test]
    public async Task GetByIdAsync_WhenFound_ReturnsDtoWithCategoryInfo()
    {
        var budget = CreateSampleBudget(3);
        _repo.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(budget);

        var result = await _sut.GetByIdAsync(3);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Id, Is.EqualTo(3));
        Assert.That(result.CategoryName, Is.EqualTo("Groceries"));
        Assert.That(result.CategoryIcon, Is.EqualTo("🛒"));
        Assert.That(result.Amount, Is.EqualTo(300m));
    }

    [Test]
    public async Task GetByIdAsync_WhenNotFound_ReturnsNull()
    {
        _repo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Budget?)null);

        var result = await _sut.GetByIdAsync(99);

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task CreateAsync_AddsThenReFetches()
    {
        var request = new CreateBudgetRequest
        {
            CategoryId = 5,
            Amount = 400m,
            Year = 2026,
            Month = 9,
            Notes = "September budget"
        };

        var created = new Budget { Id = 20, CategoryId = 5, Amount = 400m, Year = 2026, Month = 9 };
        var full = CreateSampleBudget(20);
        full.Amount = 400m;
        full.Month = 9;
        full.Notes = "September budget";

        _repo.Setup(r => r.AddAsync(It.IsAny<Budget>())).ReturnsAsync(created);
        _repo.Setup(r => r.GetByIdAsync(20)).ReturnsAsync(full);

        var result = await _sut.CreateAsync(request);

        Assert.That(result.Id, Is.EqualTo(20));
        Assert.That(result.Amount, Is.EqualTo(400m));
        Assert.That(result.CategoryName, Is.EqualTo("Groceries"));
        _repo.Verify(r => r.AddAsync(It.IsAny<Budget>()), Times.Once);
        _repo.Verify(r => r.GetByIdAsync(20), Times.Once);
    }

    [Test]
    public async Task UpdateAsync_WhenExists_UpdatesAndReturnsTrue()
    {
        var existing = CreateSampleBudget(1);
        var request = new UpdateBudgetRequest
        {
            CategoryId = 5,
            Amount = 500m,
            Year = 2026,
            Month = 8,
            Notes = "Increased budget"
        };

        _repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existing);
        _repo.Setup(r => r.UpdateAsync(It.IsAny<Budget>())).Returns(Task.CompletedTask);

        var result = await _sut.UpdateAsync(1, request);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(existing.Amount, Is.EqualTo(500m));
        Assert.That(existing.Notes, Is.EqualTo("Increased budget"));
        _repo.Verify(r => r.UpdateAsync(existing), Times.Once);
    }

    [Test]
    public async Task UpdateAsync_WhenNotFound_ReturnsFalse()
    {
        _repo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Budget?)null);

        var result = await _sut.UpdateAsync(99, new UpdateBudgetRequest
        {
            CategoryId = 1,
            Amount = 100m,
            Year = 2026,
            Month = 1
        });

        Assert.That(result.IsSuccess, Is.False);
        _repo.Verify(r => r.UpdateAsync(It.IsAny<Budget>()), Times.Never);
    }

    [Test]
    public async Task DeleteAsync_CallsRepository()
    {
        _repo.Setup(r => r.DeleteAsync(12)).ReturnsAsync(true);

        var deleted = await _sut.DeleteAsync(12);

        Assert.That(deleted, Is.True);
        _repo.Verify(r => r.DeleteAsync(12), Times.Once);
    }
}
