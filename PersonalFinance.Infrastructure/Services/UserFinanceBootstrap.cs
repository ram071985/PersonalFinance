using Microsoft.EntityFrameworkCore;
using PersonalFinance.Core.Entities;
using PersonalFinance.Core.Enums;
using PersonalFinance.Infrastructure.Data;

namespace PersonalFinance.Infrastructure.Services;

/// <summary>
/// After register:
/// 1) Assigns legacy finance rows (NULL/empty UserId) to this user.
/// 2) Seeds default categories once (by name), never duplicates.
/// </summary>
public class UserFinanceBootstrap
{
    private readonly AppDbContext _db;

    public UserFinanceBootstrap(AppDbContext db) => _db = db;

    public async Task InitializeForUserAsync(string userId)
    {
        await ClaimOrphanDataAsync(userId);
        await SeedDefaultCategoriesIfNeededAsync(userId);
        await DedupeCategoriesAsync(userId);
    }

    private async Task ClaimOrphanDataAsync(string userId)
    {
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

    private async Task SeedDefaultCategoriesIfNeededAsync(string userId)
    {
        // Ignore global UserId filter — register runs without a signed-in user.
        var existingNames = await _db.Categories
            .IgnoreQueryFilters()
            .Where(c => c.UserId == userId)
            .Select(c => c.Name)
            .ToListAsync();

        var existing = new HashSet<string>(existingNames, StringComparer.OrdinalIgnoreCase);

        var defaults = new (string Name, CategoryType Type, string Icon, string Color)[]
        {
            ("Salary", CategoryType.Income, "💵", "#22c55e"),
            ("Freelance", CategoryType.Income, "💻", "#16a34a"),
            ("Groceries", CategoryType.Expense, "🛒", "#ef4444"),
            ("Rent", CategoryType.Expense, "🏠", "#dc2626"),
            ("Utilities", CategoryType.Expense, "💡", "#f97316"),
            ("Dining Out", CategoryType.Expense, "🍽️", "#eab308"),
            ("Transportation", CategoryType.Expense, "🚗", "#3b82f6"),
            ("Entertainment", CategoryType.Expense, "🎬", "#8b5cf6"),
            ("Healthcare", CategoryType.Expense, "🏥", "#ec4899"),
            ("Shopping", CategoryType.Expense, "🛍️", "#f43f5e"),
        };

        var toAdd = defaults
            .Where(d => !existing.Contains(d.Name))
            .Select(d => new Category
            {
                UserId = userId,
                Name = d.Name,
                Type = d.Type,
                Icon = d.Icon,
                Color = d.Color,
                IsActive = true
            })
            .ToList();

        if (toAdd.Count == 0) return;

        _db.Categories.AddRange(toAdd);
        await _db.SaveChangesAsync();
    }

    /// <summary>
    /// Soft-deactivate duplicate category names for a user (keep lowest Id).
    /// </summary>
    private async Task DedupeCategoriesAsync(string userId)
    {
        var cats = await _db.Categories
            .IgnoreQueryFilters()
            .Where(c => c.UserId == userId && c.IsActive)
            .OrderBy(c => c.Id)
            .ToListAsync();

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var changed = false;
        foreach (var c in cats)
        {
            var key = $"{c.Type}:{c.Name}";
            if (!seen.Add(key))
            {
                c.IsActive = false;
                changed = true;
            }
        }

        if (changed)
            await _db.SaveChangesAsync();
    }
}
