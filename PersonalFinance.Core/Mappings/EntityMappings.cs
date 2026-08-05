using PersonalFinance.Core.Dtos.Accounts;
using PersonalFinance.Core.Dtos.Categories;
using PersonalFinance.Core.Dtos.Transactions;
using PersonalFinance.Core.Dtos.Budgets;
using PersonalFinance.Core.Dtos.Dashboard;
using PersonalFinance.Core.Entities;

namespace PersonalFinance.Core.Mappings;

public static class EntityMappings
{
// ── Dashboard ────────────────────────────────────────────
    public static DashboardDto ToDashboardDto(
        decimal netWorth,
        decimal monthlyIncome,
        decimal monthlyExpenses,
        IEnumerable<Transaction> recentTransactions,
        IEnumerable<BudgetDto>? overBudget = null) => new()
    {
        NetWorth = netWorth,
        MonthlyIncome = monthlyIncome,
        MonthlyExpenses = monthlyExpenses,
        MonthlyNet = monthlyIncome - monthlyExpenses,
        RecentTransactions = recentTransactions.ToDtoList(),
        OverBudget = overBudget?.ToList() ?? new List<BudgetDto>()
    };
    
    // ── Account ──────────────────────────────────────────────
    public static AccountDto ToDto(this Account entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        Type = entity.Type,
        Balance = entity.Balance,
        Institution = entity.Institution,
        Notes = entity.Notes,
        IsActive = entity.IsActive,
        CreatedAt = entity.CreatedAt
    };

    public static Account ToEntity(this CreateAccountRequest request) => new()
    {
        Name = request.Name,
        Type = request.Type,
        Balance = request.Balance,
        Institution = request.Institution,
        Notes = request.Notes,
        IsActive = request.IsActive,
        CreatedAt = DateTime.UtcNow
    };

    public static void ApplyUpdate(this Account entity, UpdateAccountRequest request)
    {
        entity.Name = request.Name;
        entity.Type = request.Type;
        entity.Balance = request.Balance;
        entity.Institution = request.Institution;
        entity.Notes = request.Notes;
        entity.IsActive = request.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;
    }

    // ── Category ─────────────────────────────────────────────
    public static CategoryDto ToDto(this Category entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        Type = entity.Type,
        Icon = entity.Icon,
        Color = entity.Color,
        IsActive = entity.IsActive,
        CreatedAt = entity.CreatedAt
    };

    public static Category ToEntity(this CreateCategoryRequest request) => new()
    {
        Name = request.Name,
        Type = request.Type,
        Icon = request.Icon,
        Color = request.Color,
        IsActive = request.IsActive,
        CreatedAt = DateTime.UtcNow
    };

    public static void ApplyUpdate(this Category entity, UpdateCategoryRequest request)
    {
        entity.Name = request.Name;
        entity.Type = request.Type;
        entity.Icon = request.Icon;
        entity.Color = request.Color;
        entity.IsActive = request.IsActive;
    }

    // ── Transaction ──────────────────────────────────────────
    public static TransactionDto ToDto(this Transaction entity) => new()
    {
        Id = entity.Id,
        AccountId = entity.AccountId,
        AccountName = entity.Account?.Name ?? string.Empty,
        CategoryId = entity.CategoryId,
        CategoryName = entity.Category?.Name,
        CategoryIcon = entity.Category?.Icon,
        TransferToAccountId = entity.TransferToAccountId,
        TransferToAccountName = entity.TransferToAccount?.Name,
        Amount = entity.Amount,
        Type = entity.Type,
        Description = entity.Description,
        Notes = entity.Notes,
        Date = entity.Date,
        CreatedAt = entity.CreatedAt
    };

    public static Transaction ToEntity(this CreateTransactionRequest request) => new()
    {
        AccountId = request.AccountId,
        CategoryId = request.CategoryId,
        TransferToAccountId = request.TransferToAccountId,
        Amount = request.Amount,
        Type = request.Type,
        Description = request.Description,
        Notes = request.Notes,
        Date = request.Date.Date,
        CreatedAt = DateTime.UtcNow
    };

    public static Transaction ToEntity(this UpdateTransactionRequest request, int id) => new()
    {
        Id = id,
        AccountId = request.AccountId,
        CategoryId = request.CategoryId,
        TransferToAccountId = request.TransferToAccountId,
        Amount = request.Amount,
        Type = request.Type,
        Description = request.Description,
        Notes = request.Notes,
        Date = request.Date.Date
    };

    // ── Budget ───────────────────────────────────────────────
    public static BudgetDto ToDto(this Budget entity) => new()
    {
        Id = entity.Id,
        CategoryId = entity.CategoryId,
        CategoryName = entity.Category?.Name ?? string.Empty,
        CategoryIcon = entity.Category?.Icon,
        Amount = entity.Amount,
        Year = entity.Year,
        Month = entity.Month,
        Notes = entity.Notes,
        CreatedAt = entity.CreatedAt
    };

    public static Budget ToEntity(this CreateBudgetRequest request) => new()
    {
        CategoryId = request.CategoryId,
        Amount = request.Amount,
        Year = request.Year,
        Month = request.Month,
        Notes = request.Notes,
        CreatedAt = DateTime.UtcNow
    };

    public static void ApplyUpdate(this Budget entity, UpdateBudgetRequest request)
    {
        entity.CategoryId = request.CategoryId;
        entity.Amount = request.Amount;
        entity.Year = request.Year;
        entity.Month = request.Month;
        entity.Notes = request.Notes;
    }

    // ── Collections ──────────────────────────────────────────
    public static List<AccountDto> ToDtoList(this IEnumerable<Account> entities) =>
        entities.Select(e => e.ToDto()).ToList();

    public static List<CategoryDto> ToDtoList(this IEnumerable<Category> entities) =>
        entities.Select(e => e.ToDto()).ToList();

    public static List<TransactionDto> ToDtoList(this IEnumerable<Transaction> entities) =>
        entities.Select(e => e.ToDto()).ToList();

    public static List<BudgetDto> ToDtoList(this IEnumerable<Budget> entities) =>
        entities.Select(e => e.ToDto()).ToList();
}