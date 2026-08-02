using Moq;
using PersonalFinance.Core.Common;
using PersonalFinance.Core.Dtos.Transactions;
using PersonalFinance.Core.Entities;
using PersonalFinance.Core.Enums;
using PersonalFinance.Core.Interfaces;
using PersonalFinance.Infrastructure.Services;

namespace PersonalFinance.Tests.Services;

[TestFixture]
public class TransactionServiceTests
{
    private Mock<ITransactionRepository> _repo = null!;
    private Mock<IUnitOfWork> _uow = null!;
    private TransactionService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _repo = new Mock<ITransactionRepository>();
        _uow = new Mock<IUnitOfWork>();
        // Execute the transactional callback immediately (no real DB).
        _uow.Setup(u => u.ExecuteInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task>>(),
                It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task>, CancellationToken>(
                async (op, ct) => await op(ct));
        _sut = new TransactionService(_repo.Object, _uow.Object);
    }

    private static Transaction CreateSampleTransaction(int id = 1) => new()
    {
        Id = id,
        AccountId = 10,
        CategoryId = 5,
        Amount = 42.50m,
        Type = TransactionType.Expense,
        Description = "Coffee",
        Date = DateTime.UtcNow.Date,
        CreatedAt = DateTime.UtcNow,
        Account = new Account { Id = 10, Name = "Checking" },
        Category = new Category { Id = 5, Name = "Food", Icon = "🍔" }
    };

    [Test]
    public async Task GetAllAsync_ReturnsMappedDtos()
    {
        var txns = new List<Transaction> { CreateSampleTransaction(1), CreateSampleTransaction(2) };
        _repo.Setup(r => r.GetAllAsync()).ReturnsAsync(txns);

        var result = (await _sut.GetAllAsync()).ToList();

        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(result[0].Description, Is.EqualTo("Coffee"));
        Assert.That(result[0].AccountName, Is.EqualTo("Checking"));
        Assert.That(result[0].CategoryName, Is.EqualTo("Food"));
    }

    [Test]
    public async Task GetRecentAsync_PassesCountToRepo()
    {
        var txns = new List<Transaction> { CreateSampleTransaction() };
        _repo.Setup(r => r.GetRecentAsync(5)).ReturnsAsync(txns);

        var result = (await _sut.GetRecentAsync(5)).ToList();

        Assert.That(result, Has.Count.EqualTo(1));
        _repo.Verify(r => r.GetRecentAsync(5), Times.Once);
    }

    [Test]
    public async Task GetByAccountIdAsync_FiltersByAccount()
    {
        var txns = new List<Transaction> { CreateSampleTransaction() };
        _repo.Setup(r => r.GetByAccountIdAsync(10)).ReturnsAsync(txns);

        var result = (await _sut.GetByAccountIdAsync(10)).ToList();

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].AccountId, Is.EqualTo(10));
        _repo.Verify(r => r.GetByAccountIdAsync(10), Times.Once);
    }

    [Test]
    public async Task GetByIdAsync_WhenFound_ReturnsDtoWithNames()
    {
        var txn = CreateSampleTransaction(3);
        _repo.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(txn);

        var result = await _sut.GetByIdAsync(3);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Id, Is.EqualTo(3));
        Assert.That(result.AccountName, Is.EqualTo("Checking"));
        Assert.That(result.CategoryName, Is.EqualTo("Food"));
        Assert.That(result.CategoryIcon, Is.EqualTo("🍔"));
        Assert.That(result.Amount, Is.EqualTo(42.50m));
    }

    [Test]
    public async Task GetByIdAsync_WhenNotFound_ReturnsNull()
    {
        _repo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Transaction?)null);

        var result = await _sut.GetByIdAsync(99);

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task CreateAsync_AddsThenReFetchesWithIncludes()
    {
        var request = new CreateTransactionRequest
        {
            AccountId = 10,
            CategoryId = 5,
            Amount = 25.00m,
            Type = TransactionType.Expense,
            Description = "Lunch",
            Date = DateTime.Today
        };

        var created = new Transaction { Id = 100, AccountId = 10, Amount = 25m, Description = "Lunch" };
        var full = CreateSampleTransaction(100);
        full.Description = "Lunch";
        full.Amount = 25m;

        _repo.Setup(r => r.AddAsync(It.IsAny<Transaction>())).ReturnsAsync(created);
        _repo.Setup(r => r.GetByIdAsync(100)).ReturnsAsync(full);

        var result = await _sut.CreateAsync(request);

        Assert.That(result.Id, Is.EqualTo(100));
        Assert.That(result.Description, Is.EqualTo("Lunch"));
        Assert.That(result.AccountName, Is.EqualTo("Checking"));
        _repo.Verify(r => r.AddAsync(It.IsAny<Transaction>()), Times.Once);
        _repo.Verify(r => r.GetByIdAsync(100), Times.Once);
    }

    [Test]
    public async Task UpdateAsync_WhenExists_CallsRepoUpdateAndReturnsTrue()
    {
        var existing = CreateSampleTransaction(1);
        var request = new UpdateTransactionRequest
        {
            AccountId = 10,
            CategoryId = 5,
            Amount = 50m,
            Type = TransactionType.Expense,
            Description = "Updated coffee",
            Date = DateTime.Today
        };

        _repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existing);
        _repo.Setup(r => r.UpdateAsync(It.IsAny<Transaction>())).Returns(Task.CompletedTask);

        var result = await _sut.UpdateAsync(1, request);

        Assert.That(result.IsSuccess, Is.True);
        _repo.Verify(r => r.UpdateAsync(It.Is<Transaction>(t =>
            t.Id == 1 && t.Amount == 50m && t.Description == "Updated coffee")), Times.Once);
    }

    [Test]
    public async Task UpdateAsync_WhenNotFound_ReturnsFalse()
    {
        _repo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Transaction?)null);

        var result = await _sut.UpdateAsync(99, new UpdateTransactionRequest
        {
            AccountId = 1,
            Amount = 10m,
            Description = "X",
            Date = DateTime.Today
        });

        Assert.That(result.IsSuccess, Is.False);
        _repo.Verify(r => r.UpdateAsync(It.IsAny<Transaction>()), Times.Never);
    }

    [Test]
    public async Task DeleteAsync_CallsRepository()
    {
        _repo.Setup(r => r.DeleteAsync(8)).ReturnsAsync(true);

        var deleted = await _sut.DeleteAsync(8);

        Assert.That(deleted, Is.True);
        _repo.Verify(r => r.DeleteAsync(8), Times.Once);
    }
}
