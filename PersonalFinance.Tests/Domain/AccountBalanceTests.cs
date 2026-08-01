using PersonalFinance.Core.Entities;
using PersonalFinance.Core.Enums;

namespace PersonalFinance.Tests.Domain;

[TestFixture]
public class AccountBalanceTests
{
    private static Account NewAccount(decimal balance = 1000m) => new()
    {
        Id = 1,
        Name = "Test",
        Type = AccountType.Checking,
        Balance = balance,
        UserId = "user-1"
    };

    [Test]
    public void ApplyIncome_IncreasesBalance()
    {
        var account = NewAccount(100m);
        account.ApplyIncome(50m);
        Assert.That(account.Balance, Is.EqualTo(150m));
        Assert.That(account.UpdatedAt, Is.Not.Null);
    }

    [Test]
    public void ApplyExpense_DecreasesBalance()
    {
        var account = NewAccount(100m);
        account.ApplyExpense(40m);
        Assert.That(account.Balance, Is.EqualTo(60m));
    }

    [Test]
    public void ApplyTransferOut_DecreasesBalance()
    {
        var account = NewAccount(200m);
        account.ApplyTransferOut(75m);
        Assert.That(account.Balance, Is.EqualTo(125m));
    }

    [Test]
    public void ApplyTransferIn_IncreasesBalance()
    {
        var account = NewAccount(200m);
        account.ApplyTransferIn(75m);
        Assert.That(account.Balance, Is.EqualTo(275m));
    }

    [Test]
    public void ReverseIncome_UndoesIncome()
    {
        var account = NewAccount(150m);
        account.ReverseIncome(50m);
        Assert.That(account.Balance, Is.EqualTo(100m));
    }

    [Test]
    public void ReverseExpense_UndoesExpense()
    {
        var account = NewAccount(60m);
        account.ReverseExpense(40m);
        Assert.That(account.Balance, Is.EqualTo(100m));
    }

    [Test]
    public void ApplyPrimaryEffect_Income_ThenReverse()
    {
        var account = NewAccount(100m);
        account.ApplyPrimaryEffect(TransactionType.Income, 25m, reverse: false);
        Assert.That(account.Balance, Is.EqualTo(125m));
        account.ApplyPrimaryEffect(TransactionType.Income, 25m, reverse: true);
        Assert.That(account.Balance, Is.EqualTo(100m));
    }

    [Test]
    public void ApplyIncome_RejectsNonPositiveAmount()
    {
        var account = NewAccount();
        Assert.Throws<ArgumentOutOfRangeException>(() => account.ApplyIncome(0m));
        Assert.Throws<ArgumentOutOfRangeException>(() => account.ApplyExpense(-1m));
    }
}
