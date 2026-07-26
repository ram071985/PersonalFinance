using Microsoft.EntityFrameworkCore;
using PersonalFinance.Core.Entities;
using PersonalFinance.Core.Enums;

namespace PersonalFinance.Infrastructure.Data;

public static class SeedData
{
    public static async Task InitializeAsync(AppDbContext context)
    {
        if (await context.Accounts.AnyAsync())
            return;

        var categories = new List<Category>
        {
            new() { Name = "Salary", Type = CategoryType.Income, Icon = "💵", Color = "#22c55e" },
            new() { Name = "Freelance", Type = CategoryType.Income, Icon = "💻", Color = "#16a34a" },
            new() { Name = "Groceries", Type = CategoryType.Expense, Icon = "🛒", Color = "#ef4444" },
            new() { Name = "Rent", Type = CategoryType.Expense, Icon = "🏠", Color = "#dc2626" },
            new() { Name = "Utilities", Type = CategoryType.Expense, Icon = "💡", Color = "#f97316" },
            new() { Name = "Dining Out", Type = CategoryType.Expense, Icon = "🍽️", Color = "#eab308" },
            new() { Name = "Transportation", Type = CategoryType.Expense, Icon = "🚗", Color = "#3b82f6" },
            new() { Name = "Entertainment", Type = CategoryType.Expense, Icon = "🎬", Color = "#8b5cf6" },
            new() { Name = "Healthcare", Type = CategoryType.Expense, Icon = "🏥", Color = "#ec4899" },
            new() { Name = "Shopping", Type = CategoryType.Expense, Icon = "🛍️", Color = "#f43f5e" },
        };
        context.Categories.AddRange(categories);
        await context.SaveChangesAsync();

        var accounts = new List<Account>
        {
            new() { Name = "Chase Checking", Type = AccountType.Checking, Balance = 4250.75m, Institution = "Chase" },
            new() { Name = "Ally Savings", Type = AccountType.Savings, Balance = 12800.00m, Institution = "Ally" },
            new() { Name = "Amex Blue Cash", Type = AccountType.CreditCard, Balance = -842.30m, Institution = "American Express" },
            new() { Name = "Vanguard Brokerage", Type = AccountType.Investment, Balance = 45600.00m, Institution = "Vanguard" },
            new() { Name = "Wallet Cash", Type = AccountType.Cash, Balance = 120.00m },
        };
        context.Accounts.AddRange(accounts);
        await context.SaveChangesAsync();

        var now = DateTime.UtcNow.Date;
        var transactions = new List<Transaction>
        {
            new() { AccountId = 1, CategoryId = 1, Amount = 5200.00m, Type = TransactionType.Income, Description = "Monthly salary", Date = now.AddDays(-25) },
            new() { AccountId = 1, CategoryId = 3, Amount = 142.56m, Type = TransactionType.Expense, Description = "Weekly groceries", Date = now.AddDays(-22) },
            new() { AccountId = 1, CategoryId = 4, Amount = 1850.00m, Type = TransactionType.Expense, Description = "July rent", Date = now.AddDays(-20) },
            new() { AccountId = 1, CategoryId = 5, Amount = 187.40m, Type = TransactionType.Expense, Description = "Electric + internet", Date = now.AddDays(-18) },
            new() { AccountId = 3, CategoryId = 6, Amount = 64.80m, Type = TransactionType.Expense, Description = "Dinner at Italian place", Date = now.AddDays(-15) },
            new() { AccountId = 1, CategoryId = 7, Amount = 45.00m, Type = TransactionType.Expense, Description = "Gas fill-up", Date = now.AddDays(-12) },
            new() { AccountId = 1, CategoryId = 3, Amount = 98.22m, Type = TransactionType.Expense, Description = "Trader Joe's", Date = now.AddDays(-10) },
            new() { AccountId = 1, CategoryId = null, Amount = 500.00m, Type = TransactionType.Transfer, Description = "Transfer to savings", Date = now.AddDays(-8), TransferToAccountId = 2 },
            new() { AccountId = 1, CategoryId = 8, Amount = 29.99m, Type = TransactionType.Expense, Description = "Netflix + Spotify", Date = now.AddDays(-7) },
            new() { AccountId = 3, CategoryId = 10, Amount = 156.40m, Type = TransactionType.Expense, Description = "Amazon order", Date = now.AddDays(-5) },
            new() { AccountId = 1, CategoryId = 2, Amount = 850.00m, Type = TransactionType.Income, Description = "Freelance project payment", Date = now.AddDays(-3) },
            new() { AccountId = 1, CategoryId = 6, Amount = 32.50m, Type = TransactionType.Expense, Description = "Coffee + lunch", Date = now.AddDays(-1) },
            new() { AccountId = 1, CategoryId = 3, Amount = 67.89m, Type = TransactionType.Expense, Description = "Whole Foods", Date = now },
        };
        context.Transactions.AddRange(transactions);
        await context.SaveChangesAsync();

        var year = now.Year;
        var month = now.Month;
        var budgets = new List<Budget>
        {
            new() { CategoryId = 3, Amount = 500.00m, Year = year, Month = month, Notes = "Groceries limit" },
            new() { CategoryId = 4, Amount = 1850.00m, Year = year, Month = month },
            new() { CategoryId = 5, Amount = 250.00m, Year = year, Month = month },
            new() { CategoryId = 6, Amount = 200.00m, Year = year, Month = month },
            new() { CategoryId = 7, Amount = 150.00m, Year = year, Month = month },
            new() { CategoryId = 8, Amount = 100.00m, Year = year, Month = month },
            new() { CategoryId = 10, Amount = 300.00m, Year = year, Month = month },
        };
        context.Budgets.AddRange(budgets);
        await context.SaveChangesAsync();
    }
}