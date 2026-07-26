using Microsoft.EntityFrameworkCore;
using PersonalFinance.Core.Entities;
using PersonalFinance.Core.Enums;
using PersonalFinance.Core.Interfaces;
using PersonalFinance.Infrastructure.Data;

namespace PersonalFinance.Infrastructure.Repositories;

public class TransactionRepository : ITransactionRepository
{
    private readonly AppDbContext _db;

    public TransactionRepository(AppDbContext db) => _db = db;

    public async Task<IEnumerable<Transaction>> GetAllAsync() =>
        await _db.Transactions
            .Include(t => t.Account)
            .Include(t => t.Category)
            .OrderByDescending(t => t.Date)
            .ThenByDescending(t => t.Id)
            .ToListAsync();

    public async Task<IEnumerable<Transaction>> GetByAccountIdAsync(int accountId) =>
        await _db.Transactions
            .Include(t => t.Category)
            .Where(t => t.AccountId == accountId)
            .OrderByDescending(t => t.Date)
            .ToListAsync();

    public async Task<IEnumerable<Transaction>> GetRecentAsync(int count = 10) =>
        await _db.Transactions
            .Include(t => t.Account)
            .Include(t => t.Category)
            .OrderByDescending(t => t.Date)
            .ThenByDescending(t => t.Id)
            .Take(count)
            .ToListAsync();

    public async Task<IEnumerable<Transaction>> GetByDateRangeAsync(DateTime from, DateTime to) =>
        await _db.Transactions
            .Include(t => t.Account)
            .Include(t => t.Category)
            .Where(t => t.Date >= from && t.Date <= to)
            .OrderByDescending(t => t.Date)
            .ToListAsync();

    public async Task<Transaction?> GetByIdAsync(int id) =>
        await _db.Transactions
            .Include(t => t.Account)
            .Include(t => t.Category)
            .Include(t => t.TransferToAccount)
            .FirstOrDefaultAsync(t => t.Id == id);

    public async Task<Transaction> AddAsync(Transaction transaction)
    {
        await ApplyBalanceAsync(transaction, apply: true);
        _db.Transactions.Add(transaction);
        await _db.SaveChangesAsync();
        return transaction;
    }

    public async Task UpdateAsync(Transaction input)
    {
        var existing = await _db.Transactions.FindAsync(input.Id);
        if (existing is null) return;

        await ApplyBalanceAsync(existing, apply: false);

        existing.AccountId = input.AccountId;
        existing.CategoryId = input.CategoryId;
        existing.TransferToAccountId = input.TransferToAccountId;
        existing.Amount = input.Amount;
        existing.Type = input.Type;
        existing.Description = input.Description;
        existing.Notes = input.Notes;
        existing.Date = input.Date;

        await ApplyBalanceAsync(existing, apply: true);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var transaction = await _db.Transactions.FindAsync(id);
        if (transaction is null) return;

        await ApplyBalanceAsync(transaction, apply: false);
        _db.Transactions.Remove(transaction);
        await _db.SaveChangesAsync();
    }

    public async Task<decimal> GetMonthlyIncomeAsync(int year, int month) =>
        await _db.Transactions
            .Where(t => t.Type == TransactionType.Income
                     && t.Date.Year == year
                     && t.Date.Month == month)
            .SumAsync(t => t.Amount);

    public async Task<decimal> GetMonthlyExpensesAsync(int year, int month) =>
        await _db.Transactions
            .Where(t => t.Type == TransactionType.Expense
                     && t.Date.Year == year
                     && t.Date.Month == month)
            .SumAsync(t => t.Amount);

    private async Task ApplyBalanceAsync(Transaction tx, bool apply)
    {
        var sign = apply ? 1m : -1m;
        var account = await _db.Accounts.FindAsync(tx.AccountId);
        if (account is null) return;

        if (tx.Type == TransactionType.Income)
            account.Balance += tx.Amount * sign;
        else if (tx.Type == TransactionType.Expense)
            account.Balance -= tx.Amount * sign;
        else if (tx.Type == TransactionType.Transfer && tx.TransferToAccountId.HasValue)
        {
            account.Balance -= tx.Amount * sign;
            var toAccount = await _db.Accounts.FindAsync(tx.TransferToAccountId.Value);
            if (toAccount is not null)
                toAccount.Balance += tx.Amount * sign;
        }

        account.UpdatedAt = DateTime.UtcNow;
    }
}