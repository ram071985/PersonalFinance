using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using PersonalFinance.Core.Dtos.Accounts;
using PersonalFinance.Core.Dtos.Categories;
using PersonalFinance.Core.Dtos.Transactions;
using PersonalFinance.Core.Enums;
using PersonalFinance.Web.Models;
using PersonalFinance.Web.Models;
using PersonalFinance.Web.Services;

namespace PersonalFinance.Web.Components.Pages;

public partial class Transactions : ComponentBase
{
    private List<TransactionDto> _transactions = new();
    private List<AccountDto> _accounts = new();
    private List<CategoryDto> _categories = new();
    [Inject]
    private FinanceApiClient Api { get; set; } = default!;

    [Inject]
    private ToastService Toasts { get; set; } = default!;

    [Inject]
    private ConfirmService Confirm { get; set; } = default!;

    [Inject]
    private IJSRuntime Js { get; set; } = default!;
    private TransactionFormModel _formModel = new();
    private bool _isLoading = true;
    private bool _showForm;
    private string _typeFilter = "";

    private IEnumerable<TransactionDto> FilteredTransactions =>
        string.IsNullOrEmpty(_typeFilter)
            ? _transactions
            : _transactions.Where(tx => tx.Type.ToString() == _typeFilter);


    protected override async Task OnInitializedAsync() => await LoadAsync();

    private async Task LoadAsync()
    {
        _isLoading = true;
        try
        {
            // Quiet Plaid refresh if linked (no re-auth; uses stored token)
            try { await Api.SyncAllPlaidAsync(); } catch { /* Plaid optional */ }

            _transactions = await Api.GetTransactionsAsync();
            _accounts = await Api.GetAccountsAsync();
            _categories = await Api.GetCategoriesAsync();
        }
        catch (Exception ex)
        {
            _transactions = new();
            _accounts = new();
            _categories = new();
            await Toasts.ErrorAsync($"Could not load transactions: {ex.Message}");
        }

        _isLoading = false;
    }

    private void ShowCreateForm()
    {
        _formModel = new TransactionFormModel
        {
            Date = DateTime.Today,
            Type = TransactionType.Expense,
            AccountId = _accounts.FirstOrDefault()?.Id ?? 0
        };
        _showForm = true;
    }

    private void ShowEditForm(TransactionDto tx)
    {
        _formModel = tx.ToForm();
        _showForm = true;
    }

    private void HideForm()
    {
        _showForm = false;
        _formModel = new();
    }

    private async Task HandleSaveAsync(TransactionFormModel model)
    {
        model.IsSaving = true;
        model.ErrorMessage = null;
        try
        {
            if (model.Id is null)
                await Api.CreateTransactionAsync(model.ToCreateRequest());
            else
                await Api.UpdateTransactionAsync(model.Id.Value, model.ToUpdateRequest());

            _showForm = false;
            await Toasts.SuccessAsync(model.Id is null ? "Transaction created." : "Transaction updated.");
            await LoadAsync();
        }
        catch (Exception ex)
        {
            model.ErrorMessage = ex.Message;
            await Toasts.ErrorAsync(ex.Message);
        }
        finally
        {
            model.IsSaving = false;
        }
    }

    private async Task DeleteAsync(int id)
    {
        if (!await Confirm.ShowAsync(
                "Delete this transaction? This cannot be undone.",
                title: "Delete transaction",
                confirmText: "Delete"))
            return;

        try
        {
            await Api.DeleteTransactionAsync(id);
            await Toasts.SuccessAsync("Transaction deleted.");
            await LoadAsync();
        }
        catch (Exception ex)
        {
            await Toasts.ErrorAsync(ex.Message);
        }
    }

    private async Task ExportCsvAsync()
    {
        try
        {
            var bytes = await Api.ExportTransactionsCsvAsync();
            var b64 = Convert.ToBase64String(bytes);
            // Single-quoted JS strings avoid C# quote escaping issues
            await Js.InvokeVoidAsync(
                "eval",
                $"(function(){{ var a=document.createElement('a'); a.href='data:text/csv;base64,{b64}'; a.download='transactions.csv'; a.click(); }})()");
            await Toasts.SuccessAsync("CSV downloaded.");
        }
        catch (Exception ex)
        {
            await Toasts.ErrorAsync(ex.Message);
        }
    }
}
