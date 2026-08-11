using PersonalFinance.Core.Dtos.Accounts;
using PersonalFinance.Core.Dtos.Budgets;
using PersonalFinance.Core.Dtos.Categories;
using PersonalFinance.Core.Dtos.Transactions;
using PersonalFinance.Core.Entities;
using PersonalFinance.Core.Enums;
using PersonalFinance.Core.Mappings;

namespace PersonalFinance.Tests.Mappings;

[TestFixture]
public class EntityMappingsTests
{
    // ── Account ──────────────────────────────────────────────

    [Test]
    public void Account_ToDto_MapsAllFields()
    {
        var entity = new Account
        {
            Id = 1,
            Name = "Checking",
            Type = AccountType.Checking,
            Balance = 1234.56m,
            Institution = "Test Bank",
            Notes = "Primary account",
            IsActive = true,
            CreatedAt = new DateTime(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc)
        };

        var dto = entity.ToDto();

        Assert.That(dto.Id, Is.EqualTo(1));
        Assert.That(dto.Name, Is.EqualTo("Checking"));
        Assert.That(dto.Type, Is.EqualTo(AccountType.Checking));
        Assert.That(dto.Balance, Is.EqualTo(1234.56m));
        Assert.That(dto.Institution, Is.EqualTo("Test Bank"));
        Assert.That(dto.Notes, Is.EqualTo("Primary account"));
        Assert.That(dto.IsActive, Is.True);
        Assert.That(dto.CreatedAt, Is.EqualTo(entity.CreatedAt));
    }

    [Test]
    public void CreateAccountRequest_ToEntity_MapsCorrectly()
    {
        var request = new CreateAccountRequest
        {
            Name = "Savings",
            Type = AccountType.Savings,
            Balance = 5000m,
            Institution = "Credit Union",
            Notes = "Emergency fund",
            IsActive = true
        };

        var entity = request.ToEntity();

        Assert.That(entity.Name, Is.EqualTo("Savings"));
        Assert.That(entity.Type, Is.EqualTo(AccountType.Savings));
        Assert.That(entity.Balance, Is.EqualTo(5000m));
        Assert.That(entity.Institution, Is.EqualTo("Credit Union"));
        Assert.That(entity.Notes, Is.EqualTo("Emergency fund"));
        Assert.That(entity.IsActive, Is.True);
        Assert.That(entity.CreatedAt, Is.EqualTo(DateTime.UtcNow).Within(TimeSpan.FromSeconds(2)));
    }

    [Test]
    public void Account_ApplyUpdate_UpdatesFieldsAndSetsUpdatedAt()
    {
        var entity = new Account
        {
            Id = 1,
            Name = "Old",
            Type = AccountType.Checking,
            Balance = 100m,
            IsActive = true
        };

        var request = new UpdateAccountRequest
        {
            Name = "New Name",
            Type = AccountType.Investment,
            Balance = 999m,
            Institution = "Broker",
            Notes = "Updated",
            IsActive = false
        };

        entity.ApplyUpdate(request);

        Assert.That(entity.Name, Is.EqualTo("New Name"));
        Assert.That(entity.Type, Is.EqualTo(AccountType.Investment));
        Assert.That(entity.Balance, Is.EqualTo(999m));
        Assert.That(entity.Institution, Is.EqualTo("Broker"));
        Assert.That(entity.Notes, Is.EqualTo("Updated"));
        Assert.That(entity.IsActive, Is.False);
        Assert.That(entity.UpdatedAt, Is.Not.Null);
        Assert.That(entity.UpdatedAt!.Value, Is.EqualTo(DateTime.UtcNow).Within(TimeSpan.FromSeconds(2)));
    }

    // ── Category ─────────────────────────────────────────────

    [Test]
    public void Category_ToDto_MapsAllFields()
    {
        var entity = new Category
        {
            Id = 2,
            Name = "Salary",
            Type = CategoryType.Income,
            Icon = "💰",
            Color = "#00ff00",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var dto = entity.ToDto();

        Assert.That(dto.Id, Is.EqualTo(2));
        Assert.That(dto.Name, Is.EqualTo("Salary"));
        Assert.That(dto.Type, Is.EqualTo(CategoryType.Income));
        Assert.That(dto.Icon, Is.EqualTo("💰"));
        Assert.That(dto.Color, Is.EqualTo("#00ff00"));
        Assert.That(dto.IsActive, Is.True);
    }

    [Test]
    public void CreateCategoryRequest_ToEntity_MapsCorrectly()
    {
        var request = new CreateCategoryRequest
        {
            Name = "Rent",
            Type = CategoryType.Expense,
            Icon = "🏠",
            Color = "#ff0000",
            IsActive = true
        };

        var entity = request.ToEntity();

        Assert.That(entity.Name, Is.EqualTo("Rent"));
        Assert.That(entity.Type, Is.EqualTo(CategoryType.Expense));
        Assert.That(entity.Icon, Is.EqualTo("🏠"));
        Assert.That(entity.Color, Is.EqualTo("#ff0000"));
        Assert.That(entity.IsActive, Is.True);
    }

    [Test]
    public void Category_ApplyUpdate_UpdatesFields()
    {
        var entity = new Category { Id = 1, Name = "Old", Type = CategoryType.Expense, IsActive = true };
        var request = new UpdateCategoryRequest
        {
            Name = "Utilities",
            Type = CategoryType.Expense,
            Icon = "⚡",
            Color = "#ffff00",
            IsActive = false
        };

        entity.ApplyUpdate(request);

        Assert.That(entity.Name, Is.EqualTo("Utilities"));
        Assert.That(entity.Icon, Is.EqualTo("⚡"));
        Assert.That(entity.Color, Is.EqualTo("#ffff00"));
        Assert.That(entity.IsActive, Is.False);
    }

    // ── Transaction ──────────────────────────────────────────

    [Test]
    public void Transaction_ToDto_MapsNavigationProperties()
    {
        var entity = new Transaction
        {
            Id = 10,
            AccountId = 1,
            CategoryId = 2,
            TransferToAccountId = 3,
            Amount = 75.25m,
            Type = TransactionType.Transfer,
            Description = "Transfer to savings",
            Notes = "Monthly",
            Date = new DateTime(2026, 7, 1),
            CreatedAt = DateTime.UtcNow,
            Account = new Account { Id = 1, Name = "Checking" },
            Category = new Category { Id = 2, Name = "Transfer", Icon = "↔️" },
            TransferToAccount = new Account { Id = 3, Name = "Savings" }
        };

        var dto = entity.ToDto();

        Assert.That(dto.Id, Is.EqualTo(10));
        Assert.That(dto.AccountId, Is.EqualTo(1));
        Assert.That(dto.AccountName, Is.EqualTo("Checking"));
        Assert.That(dto.CategoryId, Is.EqualTo(2));
        Assert.That(dto.CategoryName, Is.EqualTo("Transfer"));
        Assert.That(dto.CategoryIcon, Is.EqualTo("↔️"));
        Assert.That(dto.TransferToAccountId, Is.EqualTo(3));
        Assert.That(dto.TransferToAccountName, Is.EqualTo("Savings"));
        Assert.That(dto.Amount, Is.EqualTo(75.25m));
        Assert.That(dto.Type, Is.EqualTo(TransactionType.Transfer));
        Assert.That(dto.Description, Is.EqualTo("Transfer to savings"));
    }

    [Test]
    public void Transaction_ToDto_WhenNavigationsNull_UsesEmptyOrNull()
    {
        var entity = new Transaction
        {
            Id = 11,
            AccountId = 1,
            Amount = 10m,
            Type = TransactionType.Expense,
            Description = "Cash",
            Date = DateTime.Today,
            CreatedAt = DateTime.UtcNow
            // no Account / Category / TransferToAccount set
        };

        var dto = entity.ToDto();

        Assert.That(dto.AccountName, Is.EqualTo(string.Empty));
        Assert.That(dto.CategoryName, Is.Null);
        Assert.That(dto.CategoryIcon, Is.Null);
        Assert.That(dto.TransferToAccountName, Is.Null);
    }

    [Test]
    public void CreateTransactionRequest_ToEntity_MapsAndTruncatesDate()
    {
        var request = new CreateTransactionRequest
        {
            AccountId = 1,
            CategoryId = 2,
            Amount = 30m,
            Type = TransactionType.Expense,
            Description = "Lunch",
            Notes = "Team lunch",
            Date = new DateTime(2026, 8, 1, 14, 30, 0)
        };

        var entity = request.ToEntity();

        Assert.That(entity.AccountId, Is.EqualTo(1));
        Assert.That(entity.CategoryId, Is.EqualTo(2));
        Assert.That(entity.Amount, Is.EqualTo(30m));
        Assert.That(entity.Type, Is.EqualTo(TransactionType.Expense));
        Assert.That(entity.Description, Is.EqualTo("Lunch"));
        Assert.That(entity.Notes, Is.EqualTo("Team lunch"));
        Assert.That(entity.Date, Is.EqualTo(new DateTime(2026, 8, 1))); // .Date
        Assert.That(entity.CreatedAt, Is.EqualTo(DateTime.UtcNow).Within(TimeSpan.FromSeconds(2)));
    }

    [Test]
    public void UpdateTransactionRequest_ToEntity_SetsId()
    {
        var request = new UpdateTransactionRequest
        {
            AccountId = 1,
            Amount = 40m,
            Type = TransactionType.Expense,
            Description = "Updated",
            Date = DateTime.Today
        };

        var entity = request.ToEntity(55);

        Assert.That(entity.Id, Is.EqualTo(55));
        Assert.That(entity.Amount, Is.EqualTo(40m));
        Assert.That(entity.Description, Is.EqualTo("Updated"));
    }

    // ── Budget ───────────────────────────────────────────────

    [Test]
    public void Budget_ToDto_MapsCategoryInfo()
    {
        var entity = new Budget
        {
            Id = 4,
            CategoryId = 7,
            Amount = 600m,
            Year = 2026,
            Month = 8,
            Notes = "August rent",
            CreatedAt = DateTime.UtcNow,
            Category = new Category { Id = 7, Name = "Housing", Icon = "🏠" }
        };

        var dto = entity.ToDto();

        Assert.That(dto.Id, Is.EqualTo(4));
        Assert.That(dto.CategoryId, Is.EqualTo(7));
        Assert.That(dto.CategoryName, Is.EqualTo("Housing"));
        Assert.That(dto.CategoryIcon, Is.EqualTo("🏠"));
        Assert.That(dto.Amount, Is.EqualTo(600m));
        Assert.That(dto.Year, Is.EqualTo(2026));
        Assert.That(dto.Month, Is.EqualTo(8));
        Assert.That(dto.Notes, Is.EqualTo("August rent"));
    }

    [Test]
    public void CreateBudgetRequest_ToEntity_MapsCorrectly()
    {
        var request = new CreateBudgetRequest
        {
            CategoryId = 3,
            Amount = 200m,
            Year = 2026,
            Month = 12,
            Notes = "Holiday"
        };

        var entity = request.ToEntity();

        Assert.That(entity.CategoryId, Is.EqualTo(3));
        Assert.That(entity.Amount, Is.EqualTo(200m));
        Assert.That(entity.Year, Is.EqualTo(2026));
        Assert.That(entity.Month, Is.EqualTo(12));
        Assert.That(entity.Notes, Is.EqualTo("Holiday"));
    }

    [Test]
    public void Budget_ApplyUpdate_UpdatesFields()
    {
        var entity = new Budget
        {
            Id = 1,
            CategoryId = 1,
            Amount = 100m,
            Year = 2026,
            Month = 1
        };

        var request = new UpdateBudgetRequest
        {
            CategoryId = 2,
            Amount = 250m,
            Year = 2026,
            Month = 2,
            Notes = "Updated"
        };

        entity.ApplyUpdate(request);

        Assert.That(entity.CategoryId, Is.EqualTo(2));
        Assert.That(entity.Amount, Is.EqualTo(250m));
        Assert.That(entity.Month, Is.EqualTo(2));
        Assert.That(entity.Notes, Is.EqualTo("Updated"));
    }

    // ── Dashboard ────────────────────────────────────────────

    [Test]
    public void ToDashboardDto_CalculatesMonthlyNet()
    {
        var recent = new List<Transaction>
        {
            new()
            {
                Id = 1,
                AccountId = 1,
                Amount = 10m,
                Type = TransactionType.Expense,
                Description = "Test",
                Date = DateTime.Today,
                CreatedAt = DateTime.UtcNow,
                Account = new Account { Id = 1, Name = "A" }
            }
        };

        var dto = EntityMappings.ToDashboardDto(
            netWorth: 10000m,
            monthlyIncome: 5000m,
            monthlyExpenses: 1200m,
            recentTransactions: recent);

        Assert.That(dto.NetWorth, Is.EqualTo(10000m));
        Assert.That(dto.MonthlyIncome, Is.EqualTo(5000m));
        Assert.That(dto.MonthlyExpenses, Is.EqualTo(1200m));
        Assert.That(dto.MonthlyNet, Is.EqualTo(3800m));
        Assert.That(dto.RecentTransactions, Has.Count.EqualTo(1));
        Assert.That(dto.RecentTransactions[0].Description, Is.EqualTo("Test"));
    }

    // ── Collection helpers ───────────────────────────────────

    [Test]
    public void ToDtoList_Account_MapsCollection()
    {
        var list = new List<Account>
        {
            new() { Id = 1, Name = "A", Type = AccountType.Checking, Balance = 10m, IsActive = true, CreatedAt = DateTime.UtcNow },
            new() { Id = 2, Name = "B", Type = AccountType.Savings, Balance = 20m, IsActive = true, CreatedAt = DateTime.UtcNow }
        };

        var dtos = list.ToDtoList();

        Assert.That(dtos, Has.Count.EqualTo(2));
        Assert.That(dtos[0].Name, Is.EqualTo("A"));
        Assert.That(dtos[1].Name, Is.EqualTo("B"));
    }
}
