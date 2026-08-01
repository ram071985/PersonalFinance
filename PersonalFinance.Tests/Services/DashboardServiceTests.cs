using Moq;
using PersonalFinance.Core.Entities;
using PersonalFinance.Core.Enums;
using PersonalFinance.Core.Interfaces;
using PersonalFinance.Infrastructure.Services;

namespace PersonalFinance.Tests.Services;

[TestFixture]
public class DashboardServiceTests
{
    private Mock<IAccountRepository> _accounts = null!;
    private Mock<ITransactionRepository> _transactions = null!;
    private DashboardService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _accounts = new Mock<IAccountRepository>();
        _transactions = new Mock<ITransactionRepository>();
        _sut = new DashboardService(_accounts.Object, _transactions.Object);
    }

    [Test]
    public async Task GetSummaryAsync_AggregatesCorrectly()
    {
        var now = DateTime.UtcNow;
        var recent = new List<Transaction>
        {
            new()
            {
                Id = 1,
                AccountId = 1,
                Amount = 50m,
                Type = TransactionType.Expense,
                Description = "Coffee",
                Date = now.Date,
                CreatedAt = now,
                Account = new Account { Id = 1, Name = "Checking" }
            },
            new()
            {
                Id = 2,
                AccountId = 1,
                Amount = 2000m,
                Type = TransactionType.Income,
                Description = "Paycheck",
                Date = now.Date,
                CreatedAt = now,
                Account = new Account { Id = 1, Name = "Checking" }
            }
        };

        _accounts.Setup(a => a.GetTotalBalanceAsync()).ReturnsAsync(12500.75m);
        _transactions.Setup(t => t.GetMonthlyIncomeAsync(now.Year, now.Month)).ReturnsAsync(4500m);
        _transactions.Setup(t => t.GetMonthlyExpensesAsync(now.Year, now.Month)).ReturnsAsync(1800.25m);
        _transactions.Setup(t => t.GetRecentAsync(8)).ReturnsAsync(recent);

        var result = await _sut.GetSummaryAsync();

        Assert.That(result.NetWorth, Is.EqualTo(12500.75m));
        Assert.That(result.MonthlyIncome, Is.EqualTo(4500m));
        Assert.That(result.MonthlyExpenses, Is.EqualTo(1800.25m));
        Assert.That(result.MonthlyNet, Is.EqualTo(4500m - 1800.25m));
        Assert.That(result.RecentTransactions, Has.Count.EqualTo(2));
        Assert.That(result.RecentTransactions[0].Description, Is.EqualTo("Coffee"));
        Assert.That(result.RecentTransactions[1].Description, Is.EqualTo("Paycheck"));

        _accounts.Verify(a => a.GetTotalBalanceAsync(), Times.Once);
        _transactions.Verify(t => t.GetMonthlyIncomeAsync(now.Year, now.Month), Times.Once);
        _transactions.Verify(t => t.GetMonthlyExpensesAsync(now.Year, now.Month), Times.Once);
        _transactions.Verify(t => t.GetRecentAsync(8), Times.Once);
    }

    [Test]
    public async Task GetSummaryAsync_WhenNoData_ReturnsZerosAndEmptyList()
    {
        var now = DateTime.UtcNow;

        _accounts.Setup(a => a.GetTotalBalanceAsync()).ReturnsAsync(0m);
        _transactions.Setup(t => t.GetMonthlyIncomeAsync(now.Year, now.Month)).ReturnsAsync(0m);
        _transactions.Setup(t => t.GetMonthlyExpensesAsync(now.Year, now.Month)).ReturnsAsync(0m);
        _transactions.Setup(t => t.GetRecentAsync(8)).ReturnsAsync(Enumerable.Empty<Transaction>());

        var result = await _sut.GetSummaryAsync();

        Assert.That(result.NetWorth, Is.EqualTo(0m));
        Assert.That(result.MonthlyIncome, Is.EqualTo(0m));
        Assert.That(result.MonthlyExpenses, Is.EqualTo(0m));
        Assert.That(result.MonthlyNet, Is.EqualTo(0m));
        Assert.That(result.RecentTransactions, Is.Empty);
    }
}
