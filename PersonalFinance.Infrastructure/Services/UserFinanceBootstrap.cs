using Microsoft.EntityFrameworkCore;
using PersonalFinance.Core.Entities;
using PersonalFinance.Core.Enums;
using PersonalFinance.Infrastructure.Data;

namespace PersonalFinance.Infrastructure.Services;

/// <summary>
/// After register / login:
/// 1) Assigns legacy finance rows (NULL/empty UserId) to this user.
/// 2) Seeds default categories if the user has none.
/// </summary>
public class UserFinanceBootstrap
{
    private readonly AppDbContext _db;

    public UserFinanceBootstrap(AppDbContext db) => _db = db;

    public async Task InitializeForUserAsync(string userId)
    {
        await ClaimOrphanDataAsync(userId);
        await SeedDefaultCategoriesIfEmptyAsync(userId);
    }

    private async Task ClaimOrphanDataAsync(string userId)
    {
        // Raw SQL: EF non-nullable string properties don't map SQL NULL cleanly
        await _db.Database.ExecuteSqlInterpolatedAsync(
            $"""
             UPDATE pfa.Accounts SET UserId = {userId} WHERE UserId IS NULL OR UserId = ''
             """);
        await _db.Database.ExecuteSqlInterpolatedAsync(
            $"""
             UPDATE pfa.Categories SET UserId = {userId} WHERE UserId IS NULL OR UserId = ''
             """);
        await _db.Database.ExecuteSqlInterpolatedAsync(
            $"""
             UPDATE pfa.Transactions SET UserId = {userId} WHERE UserId IS NULL OR UserId = ''
             """);
        await _db.Database.ExecuteSqlInterpolatedAsync(
            $"""
             UPDATE pfa.Budgets SET UserId = {userId} WHERE UserId IS NULL OR UserId = ''
             """);
    }

    private async Task SeedDefaultCategoriesIfEmptyAsync(string userId)
    {
        var hasCategories = await _db.Categories.AnyAsync(c => c.UserId == userId);
        if (hasCategories) return;

        var defaults = new List<Category>
        {
            new() { UserId = userId, Name = "Salary", Type = CategoryType.Income, Icon = "💵", Color = "#22c55e" },
            new() { UserId = userId, Name = "Freelance", Type = CategoryType.Income, Icon = "💻", Color = "#16a34a" },
            new() { UserId = userId, Name = "Groceries", Type = CategoryType.Expense, Icon = "🛒", Color = "#ef4444" },
            new() { UserId = userId, Name = "Rent", Type = CategoryType.Expense, Icon = "🏠", Color = "#dc2626" },
            new() { UserId = userId, Name = "Utilities", Type = CategoryType.Expense, Icon = "💡", Color = "#f97316" },
            new() { UserId = userId, Name = "Dining Out", Type = CategoryType.Expense, Icon = "🍽️", Color = "#eab308" },
            new() { UserId = userId, Name = "Transportation", Type = CategoryType.Expense, Icon = "🚗", Color = "#3b82f6" },
            new() { UserId = userId, Name = "Entertainment", Type = CategoryType.Expense, Icon = "🎬", Color = "#8b5cf6" },
            new() { UserId = userId, Name = "Healthcare", Type = CategoryType.Expense, Icon = "🏥", Color = "#ec4899" },
            new() { UserId = userId, Name = "Shopping", Type = CategoryType.Expense, Icon = "🛍️", Color = "#f43f5e" },
        };

        _db.Categories.AddRange(defaults);
        await _db.SaveChangesAsync();
    }
}
