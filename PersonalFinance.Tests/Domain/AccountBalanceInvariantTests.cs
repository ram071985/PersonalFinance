using PersonalFinance.Core.Entities;
using PersonalFinance.Core.Enums;

namespace PersonalFinance.Tests.Domain;

[TestFixture]
public class AccountBalanceInvariantTests
{
    [TestCase(100, 25, 75)]
    [TestCase(100, 100, 0)]
    [TestCase(50.50, 0.50, 50.00)]
    public void Expense_ThenReverse_RestoresBalance(decimal start, decimal amount, decimal afterExpense)
    {
        var account = new Account { Balance = start };

        account.ApplyPrimaryEffect(TransactionType.Expense, amount, reverse: false);
        Assert.That(account.Balance, Is.EqualTo(afterExpense));

        account.ApplyPrimaryEffect(TransactionType.Expense, amount, reverse: true);
        Assert.That(account.Balance, Is.EqualTo(start));
    }

    [TestCase(0, 40, 40)]
    [TestCase(10, 5.25, 15.25)]
    public void Income_ThenReverse_RestoresBalance(decimal start, decimal amount, decimal afterIncome)
    {
        var account = new Account { Balance = start };

        account.ApplyPrimaryEffect(TransactionType.Income, amount, reverse: false);
        Assert.That(account.Balance, Is.EqualTo(afterIncome));

        account.ApplyPrimaryEffect(TransactionType.Income, amount, reverse: true);
        Assert.That(account.Balance, Is.EqualTo(start));
    }

    [Test]
    public void Transfer_RoundTrip_PreservesTotal()
    {
        var from = new Account { Balance = 200 };
        var to = new Account { Balance = 50 };
        const decimal amount = 30m;
        var totalBefore = from.Balance + to.Balance;

        from.ApplyTransferOut(amount);
        to.ApplyTransferIn(amount);
        Assert.That(from.Balance + to.Balance, Is.EqualTo(totalBefore));

        from.ApplyPrimaryEffect(TransactionType.Transfer, amount, reverse: true);
        // reverse transfer-in manually via opposite direction
        to.Balance -= amount;
        to.UpdatedAt = DateTime.UtcNow;

        Assert.That(from.Balance, Is.EqualTo(200));
        Assert.That(to.Balance, Is.EqualTo(50));
    }
}