using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PersonalFinance.Core.Dtos.Plaid;
using PersonalFinance.Core.Entities;
using PersonalFinance.Core.Enums;
using PersonalFinance.Core.Interfaces;
using PersonalFinance.Infrastructure.Data;
using PersonalFinance.Infrastructure.Services;

namespace PersonalFinance.Infrastructure.Plaid;

public class PlaidService : IPlaidService
{
    private readonly PlaidApiClient _api;
    private readonly AppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly PlaidTokenProtector _tokens;
    private readonly PlaidOptions _options;
    private readonly ILogger<PlaidService> _logger;

    public PlaidService(
        PlaidApiClient api,
        AppDbContext db,
        ICurrentUserService currentUser,
        PlaidTokenProtector tokens,
        IOptions<PlaidOptions> options,
        ILogger<PlaidService> logger)
    {
        _api = api;
        _db = db;
        _currentUser = currentUser;
        _tokens = tokens;
        _options = options.Value;
        _logger = logger;
    }

    private string UserId =>
        _currentUser.UserId ?? throw new UnauthorizedAccessException("Authenticated user required.");

    public async Task<PlaidLinkTokenResponse> CreateLinkTokenAsync(CancellationToken ct = default)
    {
        EnsureEnabled();
        var body = new Dictionary<string, object?>
        {
            ["user"] = new { client_user_id = UserId },
            ["client_name"] = "Personal Finance",
            ["products"] = new[] { "transactions" },
            ["country_codes"] = new[] { "US" },
            ["language"] = "en"
        };
        if (!string.IsNullOrWhiteSpace(_options.WebhookUrl))
            body["webhook"] = _options.WebhookUrl;

        var json = await _api.PostAsync("/link/token/create", body, ct);
        return new PlaidLinkTokenResponse
        {
            LinkToken = json.GetProperty("link_token").GetString()!,
            Expiration = json.TryGetProperty("expiration", out var exp) ? exp.GetString() ?? "" : ""
        };
    }

    public async Task<PlaidItemDto> ExchangePublicTokenAsync(PlaidExchangeRequest request, CancellationToken ct = default)
    {
        EnsureEnabled();
        if (string.IsNullOrWhiteSpace(request.PublicToken))
            throw new ArgumentException("PublicToken is required.");

        var json = await _api.PostAsync("/item/public_token/exchange", new
        {
            public_token = request.PublicToken
        }, ct);

        var accessToken = json.GetProperty("access_token").GetString()!;
        var itemId = json.GetProperty("item_id").GetString()!;

        // Never log accessToken
        var item = new PlaidItem
        {
            UserId = UserId,
            ItemId = itemId,
            AccessTokenProtected = _tokens.Protect(accessToken),
            InstitutionId = request.InstitutionId,
            InstitutionName = request.InstitutionName,
            Status = "active",
            CreatedAt = DateTime.UtcNow
        };

        _db.PlaidItems.Add(item);
        await _db.SaveChangesAsync(ct);

        // Initial account + transaction sync
        try
        {
            await SyncCoreAsync(item, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Initial Plaid sync failed for Item {ItemId}", itemId);
            item.LastError = ex.Message.Length > 500 ? ex.Message[..500] : ex.Message;
            await _db.SaveChangesAsync(ct);
        }

        return ToDto(item);
    }

    public async Task<IReadOnlyList<PlaidItemDto>> GetItemsAsync(CancellationToken ct = default)
    {
        var items = await _db.PlaidItems
            .Where(i => i.UserId == UserId)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync(ct);
        return items.Select(ToDto).ToList();
    }

    public async Task<PlaidSyncResultDto> SyncItemAsync(int plaidItemId, CancellationToken ct = default)
    {
        var item = await _db.PlaidItems.FirstOrDefaultAsync(i => i.Id == plaidItemId && i.UserId == UserId, ct)
                   ?? throw new InvalidOperationException("Plaid connection not found.");
        return await SyncCoreAsync(item, ct);
    }

    public async Task<PlaidSyncResultDto> SyncAllForCurrentUserAsync(CancellationToken ct = default)
    {
        var items = await _db.PlaidItems.Where(i => i.UserId == UserId && i.Status == "active").ToListAsync(ct);
        var total = new PlaidSyncResultDto();
        foreach (var item in items)
        {
            var r = await SyncCoreAsync(item, ct);
            total.AccountsUpserted += r.AccountsUpserted;
            total.TransactionsAdded += r.TransactionsAdded;
            total.TransactionsModified += r.TransactionsModified;
            total.TransactionsRemoved += r.TransactionsRemoved;
            total.LastSyncedAt = r.LastSyncedAt;
        }
        return total;
    }

    public async Task<bool> RemoveItemAsync(int plaidItemId, CancellationToken ct = default)
    {
        var item = await _db.PlaidItems.FirstOrDefaultAsync(i => i.Id == plaidItemId && i.UserId == UserId, ct);
        if (item is null) return false;

        try
        {
            var access = _tokens.Unprotect(item.AccessTokenProtected);
            await _api.PostAsync("/item/remove", new { access_token = access }, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Plaid item/remove failed for {ItemId}", item.ItemId);
        }

        // Soft-detach accounts
        var accounts = await _db.Accounts
            .IgnoreQueryFilters()
            .Where(a => a.UserId == item.UserId && a.PlaidItemId == item.Id)
            .ToListAsync(ct);
        foreach (var a in accounts)
        {
            a.ExternalId = null;
            a.PlaidItemId = null;
        }

        _db.PlaidItems.Remove(item);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task SyncByPlaidItemIdAsync(string plaidItemId, CancellationToken ct = default)
    {
        var item = await _db.PlaidItems
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(i => i.ItemId == plaidItemId, ct);
        if (item is null)
        {
            _logger.LogDebug("Webhook item {ItemId} not found", plaidItemId);
            return;
        }

        using (CurrentUserService.Impersonate(item.UserId))
        {
            await SyncCoreAsync(item, ct);
        }
    }

    private async Task<PlaidSyncResultDto> SyncCoreAsync(PlaidItem item, CancellationToken ct)
    {
        EnsureEnabled();
        var result = new PlaidSyncResultDto();
        var access = _tokens.Unprotect(item.AccessTokenProtected);

        // ── Accounts ─────────────────────────────────────────
        var accountsJson = await _api.PostAsync("/accounts/get", new { access_token = access }, ct);
        var accountMap = new Dictionary<string, Account>(); // plaid account_id -> local

        if (accountsJson.TryGetProperty("accounts", out var accountsEl))
        {
            foreach (var a in accountsEl.EnumerateArray())
            {
                var plaidAccountId = a.GetProperty("account_id").GetString()!;
                var name = a.TryGetProperty("name", out var n) ? n.GetString() ?? "Account" : "Account";
                var official = a.TryGetProperty("official_name", out var on) ? on.GetString() : null;
                var type = MapAccountType(
                    a.TryGetProperty("type", out var t) ? t.GetString() : null,
                    a.TryGetProperty("subtype", out var st) ? st.GetString() : null);
                var balance = 0m;
                if (a.TryGetProperty("balances", out var bal) &&
                    bal.TryGetProperty("current", out var cur) &&
                    cur.ValueKind == JsonValueKind.Number)
                    balance = cur.GetDecimal();

                var existing = await _db.Accounts
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(x => x.UserId == item.UserId && x.ExternalId == plaidAccountId, ct);

                if (existing is null)
                {
                    existing = new Account
                    {
                        UserId = item.UserId,
                        Name = official ?? name,
                        Type = type,
                        Balance = balance,
                        Institution = item.InstitutionName,
                        ExternalId = plaidAccountId,
                        PlaidItemId = item.Id,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    };
                    _db.Accounts.Add(existing);
                    result.AccountsUpserted++;
                }
                else
                {
                    existing.Balance = balance;
                    existing.Institution = item.InstitutionName ?? existing.Institution;
                    existing.PlaidItemId = item.Id;
                    existing.UpdatedAt = DateTime.UtcNow;
                    result.AccountsUpserted++;
                }

                await _db.SaveChangesAsync(ct);
                accountMap[plaidAccountId] = existing;
            }
        }

        // ── Transactions sync (cursor-based) ─────────────────
        var cursor = item.SyncCursor;
        var hasMore = true;
        while (hasMore)
        {
            var body = new Dictionary<string, object?>
            {
                ["access_token"] = access,
                ["cursor"] = cursor,
                ["count"] = 500
            };
            var syncJson = await _api.PostAsync("/transactions/sync", body, ct);

            if (syncJson.TryGetProperty("added", out var added))
            {
                foreach (var tx in added.EnumerateArray())
                {
                    if (await UpsertTransactionAsync(item.UserId, accountMap, tx, ct))
                        result.TransactionsAdded++;
                }
            }

            if (syncJson.TryGetProperty("modified", out var modified))
            {
                foreach (var tx in modified.EnumerateArray())
                {
                    if (await UpsertTransactionAsync(item.UserId, accountMap, tx, ct))
                        result.TransactionsModified++;
                }
            }

            if (syncJson.TryGetProperty("removed", out var removed))
            {
                foreach (var tx in removed.EnumerateArray())
                {
                    if (tx.TryGetProperty("transaction_id", out var tid))
                    {
                        var id = tid.GetString();
                        var local = await _db.Transactions
                            .IgnoreQueryFilters()
                            .FirstOrDefaultAsync(x => x.UserId == item.UserId && x.ExternalId == id, ct);
                        if (local is not null && !local.IsDeleted)
                        {
                            local.IsDeleted = true;
                            local.DeletedAt = DateTime.UtcNow;
                            result.TransactionsRemoved++;
                        }
                    }
                }
            }

            cursor = syncJson.TryGetProperty("next_cursor", out var nc) ? nc.GetString() : cursor;
            hasMore = syncJson.TryGetProperty("has_more", out var hm) && hm.GetBoolean();
            await _db.SaveChangesAsync(ct);
        }

        item.SyncCursor = cursor;
        item.LastSyncedAt = DateTime.UtcNow;
        item.LastError = null;
        item.Status = "active";
        await _db.SaveChangesAsync(ct);

        result.LastSyncedAt = item.LastSyncedAt;
        _logger.LogInformation(
            "Plaid sync complete. UserId={UserId} ItemId={ItemId} Added={Added} Modified={Modified} Removed={Removed}",
            item.UserId, item.ItemId, result.TransactionsAdded, result.TransactionsModified, result.TransactionsRemoved);

        return result;
    }

    private async Task<bool> UpsertTransactionAsync(
        string userId,
        Dictionary<string, Account> accountMap,
        JsonElement tx,
        CancellationToken ct)
    {
        var txId = tx.GetProperty("transaction_id").GetString()!;
        var accountId = tx.GetProperty("account_id").GetString()!;
        if (!accountMap.TryGetValue(accountId, out var account))
        {
            account = await _db.Accounts
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(a => a.UserId == userId && a.ExternalId == accountId, ct);
            if (account is null) return false;
            accountMap[accountId] = account;
        }

        // Plaid: positive amount = money leaving account (expense)
        var amount = tx.GetProperty("amount").GetDecimal();
        var name = tx.TryGetProperty("name", out var nm) ? nm.GetString() ?? "Transaction" : "Transaction";
        var dateStr = tx.TryGetProperty("date", out var d) ? d.GetString() : null;
        var date = DateTime.TryParse(dateStr, out var parsed) ? parsed.Date : DateTime.UtcNow.Date;
        var pending = tx.TryGetProperty("pending", out var p) && p.GetBoolean();
        if (pending) return false; // wait for posted

        var type = amount >= 0 ? TransactionType.Expense : TransactionType.Income;
        var absAmount = Math.Abs(amount);

        var existing = await _db.Transactions
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.UserId == userId && t.ExternalId == txId, ct);

        if (existing is null)
        {
            // Balance already set from accounts/get — avoid double-applying on import
            _db.Transactions.Add(new Transaction
            {
                UserId = userId,
                AccountId = account.Id,
                Amount = absAmount,
                Type = type,
                Description = name.Length > 200 ? name[..200] : name,
                Date = date,
                ExternalId = txId,
                CreatedAt = DateTime.UtcNow
            });
            return true;
        }

        existing.Amount = absAmount;
        existing.Type = type;
        existing.Description = name.Length > 200 ? name[..200] : name;
        existing.Date = date;
        existing.IsDeleted = false;
        existing.DeletedAt = null;
        return true;
    }

    private static AccountType MapAccountType(string? type, string? subtype)
    {
        var t = (type ?? "").ToLowerInvariant();
        var s = (subtype ?? "").ToLowerInvariant();
        if (t == "credit") return AccountType.CreditCard;
        if (t == "investment") return AccountType.Investment;
        if (t == "loan") return AccountType.Loan;
        if (s is "checking" or "prepaid") return AccountType.Checking;
        if (s is "savings" or "money market" or "cd") return AccountType.Savings;
        if (t == "depository") return AccountType.Checking;
        return AccountType.Other;
    }

    private void EnsureEnabled()
    {
        if (!_options.Enabled ||
            string.IsNullOrWhiteSpace(_options.ClientId) ||
            string.IsNullOrWhiteSpace(_options.Secret))
            throw new InvalidOperationException("Plaid is not configured. Set Plaid:Enabled, ClientId, and Secret.");
    }

    private static PlaidItemDto ToDto(PlaidItem i) => new()
    {
        Id = i.Id,
        ItemId = i.ItemId,
        InstitutionName = i.InstitutionName,
        Status = i.Status,
        CreatedAt = i.CreatedAt,
        LastSyncedAt = i.LastSyncedAt,
        LastError = i.LastError
    };
}
