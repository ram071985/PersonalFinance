using PersonalFinance.Core.Dtos.Accounts;
using PersonalFinance.Core.Dtos.Budgets;
using PersonalFinance.Core.Dtos.Categories;
using PersonalFinance.Core.Dtos.Transactions;
using PersonalFinance.Web.Helpers;

namespace PersonalFinance.Web.Models;

public static class FormMappings
{
    // ── Account ──────────────────────────────────────────────
    public static AccountFormModel ToForm(this AccountDto dto) => new()
    {
        Id = dto.Id,
        Name = dto.Name,
        Type = dto.Type,
        Balance = dto.Balance,
        Institution = dto.Institution,
        Notes = dto.Notes,
        IsActive = dto.IsActive
    };

    public static CreateAccountRequest ToCreateRequest(this AccountFormModel form) => new()
    {
        Name = form.Name,
        Type = form.Type,
        Balance = CurrencyFormat.Sanitize(form.Balance.ToString(System.Globalization.CultureInfo.InvariantCulture)),
        Institution = form.Institution,
        Notes = form.Notes,
        IsActive = form.IsActive
    };

    public static UpdateAccountRequest ToUpdateRequest(this AccountFormModel form) => new()
    {
        Name = form.Name,
        Type = form.Type,
        Balance = CurrencyFormat.Sanitize(form.Balance.ToString(System.Globalization.CultureInfo.InvariantCulture)),
        Institution = form.Institution,
        Notes = form.Notes,
        IsActive = form.IsActive
    };

    // ── Transaction ──────────────────────────────────────────
    public static TransactionFormModel ToForm(this TransactionDto dto) => new()
    {
        Id = dto.Id,
        AccountId = dto.AccountId,
        CategoryId = dto.CategoryId,
        TransferToAccountId = dto.TransferToAccountId,
        Amount = dto.Amount,
        Type = dto.Type,
        Description = dto.Description,
        Notes = dto.Notes,
        Date = dto.Date
    };

    public static CreateTransactionRequest ToCreateRequest(this TransactionFormModel form) => new()
    {
        AccountId = form.AccountId,
        CategoryId = form.CategoryId,
        TransferToAccountId = form.TransferToAccountId,
        Amount = CurrencyFormat.Sanitize(form.Amount.ToString(System.Globalization.CultureInfo.InvariantCulture)),
        Type = form.Type,
        Description = form.Description,
        Notes = form.Notes,
        Date = form.Date
    };

    public static UpdateTransactionRequest ToUpdateRequest(this TransactionFormModel form) => new()
    {
        AccountId = form.AccountId,
        CategoryId = form.CategoryId,
        TransferToAccountId = form.TransferToAccountId,
        Amount = CurrencyFormat.Sanitize(form.Amount.ToString(System.Globalization.CultureInfo.InvariantCulture)),
        Type = form.Type,
        Description = form.Description,
        Notes = form.Notes,
        Date = form.Date
    };

    // ── Category ─────────────────────────────────────────────
    public static CategoryFormModel ToForm(this CategoryDto dto) => new()
    {
        Id = dto.Id,
        Name = dto.Name,
        Type = dto.Type,
        Icon = dto.Icon,
        Color = dto.Color,
        IsActive = dto.IsActive
    };

    public static CreateCategoryRequest ToCreateRequest(this CategoryFormModel form) => new()
    {
        Name = form.Name,
        Type = form.Type,
        Icon = form.Icon,
        Color = form.Color,
        IsActive = form.IsActive
    };

    public static UpdateCategoryRequest ToUpdateRequest(this CategoryFormModel form) => new()
    {
        Name = form.Name,
        Type = form.Type,
        Icon = form.Icon,
        Color = form.Color,
        IsActive = form.IsActive
    };

    // ── Budget ───────────────────────────────────────────────
    public static BudgetFormModel ToForm(this BudgetDto dto) => new()
    {
        Id = dto.Id,
        CategoryId = dto.CategoryId,
        Amount = dto.Amount,
        Year = dto.Year,
        Month = dto.Month,
        Notes = dto.Notes
    };

    public static CreateBudgetRequest ToCreateRequest(this BudgetFormModel form) => new()
    {
        CategoryId = form.CategoryId,
        Amount = CurrencyFormat.Sanitize(form.Amount.ToString(System.Globalization.CultureInfo.InvariantCulture)),
        Year = form.Year,
        Month = form.Month,
        Notes = form.Notes
    };

    public static UpdateBudgetRequest ToUpdateRequest(this BudgetFormModel form) => new()
    {
        CategoryId = form.CategoryId,
        Amount = CurrencyFormat.Sanitize(form.Amount.ToString(System.Globalization.CultureInfo.InvariantCulture)),
        Year = form.Year,
        Month = form.Month,
        Notes = form.Notes
    };
}
