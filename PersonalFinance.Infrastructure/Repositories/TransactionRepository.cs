using Microsoft.EntityFrameworkCore;
using PersonalFinance.Core.Entities;
using PersonalFinance.Core.Enums;
using PersonalFinance.Core.Interfaces;
using PersonalFinance.Infrastructure.Data;

namespace PersonalFinance.Infrastructure.Repositories;

public class TransactionRepository : ITransactionRepository
{
    private readonly AppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public TransactionRepository(AppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    private string UserId =>
        _currentUser.UserId
        ?? throw new UnauthorizedAccessException("Authenticated user is required.");

    public async Task<IEnumerable<Transaction>> GetAllAsync() =>
        await _db.Transactions
            .Include(t => t.Account)
            .Include(t => t.Category)
            .Where(t => t.UserId == UserId)
            .OrderByDescending(t => t.Date)
            .ThenByDescending(t => t.Id)
            .ToListAsync();

    public async Task<IEnumerable<Transaction>> GetByAccountIdAsync(int accountId) =>
        await _db.Transactions
            .Include(t => t.Category)
            .Where(t => t.UserId == UserId && t.AccountId == accountId)
            .OrderByDescending(t => t.Date)
            .ToListAsync();

    public async Task<IEnumerable<Transaction>> GetRecentAsync(int count = 10) =>
        await _db.Transactions
            .Include(t => t.Account)
            .Include(t => t.Category)
            .Where(t => t.UserId == UserId)
            .OrderByDescending(t => t.Date)
            .ThenByDescending(t => t.Id)
            .Take(count)
            .ToListAsync();

    public async Task<IEnumerable<Transaction>> GetByDateRangeAsync(DateTime from, DateTime to) =>
        await _db.Transactions
            .Include(t => t.Account)
            .Include(t => t.Category)
            .Where(t => t.UserId == UserId && t.Date >= from && t.Date <= to)
            .OrderByDescending(t => t.Date)
            .ToListAsync();

    public async Task<Transaction?> GetByIdAsync(int id) =>
        await _db.Transactions
            .Include(t => t.Account)
            .Include(t => t.Category)
            .Include(t => t.TransferToAccount)
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == UserId);

    public async Task<Transaction> AddAsync(Transaction transaction)
    {
        transaction.UserId = UserId;
        transaction.CreatedAt = DateTime.UtcNow;

        await EnsureAccountOwnershipAsync(transaction.AccountId);
        if (transaction.TransferToAccountId.HasValue)
            await EnsureAccountOwnershipAsync(transaction.TransferToAccountId.Value);
        if (transaction.CategoryId.HasValue)
            await EnsureCategoryOwnershipAsync(transaction.CategoryId.Value);

        await ApplyBalanceAsync(transaction, apply: true);
        _db.Transactions.Add(transaction);
        await _db.SaveChangesAsync();
        return transaction;
    }

    public async Task UpdateAsync(Transaction input)
    {
        var existing = await GetByIdAsync(input.Id);
        if (existing is null) return;

        await EnsureAccountOwnershipAsync(input.AccountId);
        if (input.TransferToAccountId.HasValue)
            await EnsureAccountOwnershipAsync(input.TransferToAccountId.Value);
        if (input.CategoryId.HasValue)
            await EnsureCategoryOwnershipAsync(input.CategoryId.Value);

        // Reverse old balance effect
        await ApplyBalanceAsync(existing, apply: false);

        existing.AccountId = input.AccountId;
        existing.CategoryId = input.CategoryId;
        existing.TransferToAccountId = input.TransferToAccountId;
        existing.Amount = input.Amount;
        existing.Type = input.Type;
        existing.Description = input.Description;
        existing.Notes = input.Notes;
        existing.Date = input.Date;

        // Apply new balance effect
        await ApplyBalanceAsync(existing, apply: true);
        await _db.SaveChangesAsync();
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var transaction = await GetByIdAsync(id);
        if (transaction is null) return false;

        await ApplyBalanceAsync(transaction, apply: false);
        _db.Transactions.Remove(transaction);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<decimal> GetMonthlyIncomeAsync(int year, int month) =>
        await _db.Transactions
            .Where(t => t.UserId == UserId
                     && t.Type == TransactionType.Income
                     && t.Date.Year == year
                     && t.Date.Month == month)
            .SumAsync(t => t.Amount);

    public async Task<decimal> GetMonthlyExpensesAsync(int year, int month) =>
        await _db.Transactions
            .Where(t => t.UserId == UserId
                     && t.Type == TransactionType.Expense
                     && t.Date.Year == year
                     && t.Date.Month == month)
            .SumAsync(t => t.Amount);

    private async Task EnsureAccountOwnershipAsync(int accountId)
    {
        var owns = await _db.Accounts
            .AnyAsync(a => a.Id == accountId && a.UserId == UserId);

        if (!owns)
            throw new UnauthorizedAccessException("Account does not belong to the current user.");
    }

    private async Task EnsureCategoryOwnershipAsync(int categoryId)
    {
        var owns = await _db.Categories
            .AnyAsync(c => c.Id == categoryId && c.UserId == UserId && c.IsActive);

        if (!owns)
            throw new UnauthorizedAccessException("Category does not belong to the current user.");
    }

    /// <summary>
    /// Loads owned accounts and applies domain balance methods on Account.
    /// </summary>
    private async Task ApplyBalanceAsync(Transaction tx, bool apply)
    {
        var reverse = !apply;
        var account = await _db.Accounts
            .FirstOrDefaultAsync(a => a.Id == tx.AccountId && a.UserId == UserId);

        if (account is null) return;

        account.ApplyPrimaryEffect(tx.Type, tx.Amount, reverse);

        if (tx.Type == TransactionType.Transfer && tx.TransferToAccountId.HasValue)
        {
            var toAccount = await _db.Accounts
                .FirstOrDefaultAsync(a => a.Id == tx.TransferToAccountId.Value && a.UserId == UserId);
            if (toAccount is null) return;

            if (reverse)
                toAccount.ReverseTransferIn(tx.Amount);
            else
                toAccount.ApplyTransferIn(tx.Amount);
        }
    }
}

