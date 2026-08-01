using Moq;
using PersonalFinance.Core.Common;
using PersonalFinance.Core.Dtos.Accounts;
using PersonalFinance.Core.Entities;
using PersonalFinance.Core.Enums;
using PersonalFinance.Core.Interfaces;
using PersonalFinance.Infrastructure.Services;

namespace PersonalFinance.Tests.Services;

[TestFixture]
public class AccountServiceTests
{
    private Mock<IAccountRepository> _repo = null!;
    private AccountService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _repo = new Mock<IAccountRepository>();
        _sut = new AccountService(_repo.Object);
    }

    [Test]
    public async Task GetAllAsync_ReturnsMappedDtos()
    {
        var accounts = new List<Account>
        {
            new() { Id = 1, Name = "Checking", Type = AccountType.Checking, Balance = 1000m, IsActive = true, CreatedAt = DateTime.UtcNow },
            new() { Id = 2, Name = "Savings", Type = AccountType.Savings, Balance = 5000m, IsActive = true, CreatedAt = DateTime.UtcNow }
        };

        _repo.Setup(r => r.GetAllAsync()).ReturnsAsync(accounts);

        var result = (await _sut.GetAllAsync()).ToList();

        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(result[0].Name, Is.EqualTo("Checking"));
        Assert.That(result[0].Balance, Is.EqualTo(1000m));
        Assert.That(result[1].Name, Is.EqualTo("Savings"));
        _repo.Verify(r => r.GetAllAsync(), Times.Once);
    }

    [Test]
    public async Task GetByIdAsync_WhenFound_ReturnsDto()
    {
        var account = new Account
        {
            Id = 1,
            Name = "Checking",
            Type = AccountType.Checking,
            Balance = 1500.50m,
            Institution = "Bank of Test",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(account);

        var result = await _sut.GetByIdAsync(1);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Id, Is.EqualTo(1));
        Assert.That(result.Name, Is.EqualTo("Checking"));
        Assert.That(result.Balance, Is.EqualTo(1500.50m));
        Assert.That(result.Institution, Is.EqualTo("Bank of Test"));
    }

    [Test]
    public async Task GetByIdAsync_WhenNotFound_ReturnsNull()
    {
        _repo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Account?)null);

        var result = await _sut.GetByIdAsync(99);

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task CreateAsync_MapsRequestAndReturnsDto()
    {
        var request = new CreateAccountRequest
        {
            Name = "New Checking",
            Type = AccountType.Checking,
            Balance = 200m,
            Institution = "Test Bank",
            Notes = "Primary",
            IsActive = true
        };

        var createdEntity = new Account
        {
            Id = 10,
            Name = request.Name,
            Type = request.Type,
            Balance = request.Balance,
            Institution = request.Institution,
            Notes = request.Notes,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _repo.Setup(r => r.AddAsync(It.IsAny<Account>()))
             .ReturnsAsync(createdEntity);

        var result = await _sut.CreateAsync(request);

        Assert.That(result.Id, Is.EqualTo(10));
        Assert.That(result.Name, Is.EqualTo("New Checking"));
        Assert.That(result.Balance, Is.EqualTo(200m));
        _repo.Verify(r => r.AddAsync(It.Is<Account>(a =>
            a.Name == "New Checking" &&
            a.Type == AccountType.Checking &&
            a.Balance == 200m)), Times.Once);
    }

    [Test]
    public async Task UpdateAsync_WhenExists_UpdatesAndReturnsTrue()
    {
        var existing = new Account
        {
            Id = 1,
            Name = "Old Name",
            Type = AccountType.Checking,
            Balance = 100m,
            IsActive = true
        };

        var request = new UpdateAccountRequest
        {
            Name = "Updated Name",
            Type = AccountType.Savings,
            Balance = 250m,
            Institution = "New Bank",
            IsActive = true
        };

        _repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existing);
        _repo.Setup(r => r.UpdateAsync(It.IsAny<Account>())).Returns(Task.CompletedTask);

        var result = await _sut.UpdateAsync(1, request);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(existing.Name, Is.EqualTo("Updated Name"));
        Assert.That(existing.Type, Is.EqualTo(AccountType.Savings));
        Assert.That(existing.Balance, Is.EqualTo(250m));
        Assert.That(existing.UpdatedAt, Is.Not.Null);
        _repo.Verify(r => r.UpdateAsync(existing), Times.Once);
    }

    [Test]
    public async Task UpdateAsync_WhenNotFound_ReturnsFalse()
    {
        _repo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Account?)null);

        var result = await _sut.UpdateAsync(99, new UpdateAccountRequest { Name = "X" });

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error, Is.EqualTo("Account not found."));
        _repo.Verify(r => r.UpdateAsync(It.IsAny<Account>()), Times.Never);
    }

    [Test]
    public async Task DeleteAsync_CallsRepository()
    {
        _repo.Setup(r => r.DeleteAsync(5)).ReturnsAsync(true);

        var deleted = await _sut.DeleteAsync(5);

        Assert.That(deleted, Is.True);
        _repo.Verify(r => r.DeleteAsync(5), Times.Once);
    }

    [Test]
    public async Task GetTotalBalanceAsync_ReturnsValueFromRepo()
    {
        _repo.Setup(r => r.GetTotalBalanceAsync()).ReturnsAsync(12345.67m);

        var result = await _sut.GetTotalBalanceAsync();

        Assert.That(result, Is.EqualTo(12345.67m));
    }
}
