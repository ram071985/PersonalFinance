using Microsoft.AspNetCore.Components;
using PersonalFinance.Core.Dtos.Accounts;
using PersonalFinance.Core.Enums;
using PersonalFinance.Web.Models;
using PersonalFinance.Web.Services;

namespace PersonalFinance.Web.Components.Pages;

public partial class Accounts : ComponentBase
{
    private List<AccountDto> _accounts = new();
    private AccountFormModel _formModel = new();
    private bool _isLoading = true;
    private bool _showForm;
    [Inject] private ToastService Toasts { get; set; } = default!;

    protected override async Task OnInitializedAsync() => await LoadAsync();

    private async Task LoadAsync()
    {
        _isLoading = true;
        try
        {
            _accounts = await Api.GetAccountsAsync();
        }
        catch (Exception ex)
        {
            _accounts = new();
            await Toasts.ErrorAsync($"Could not load accounts: {ex.Message}");
        }
        _isLoading = false;
    }

    private void ShowCreateForm()
    {
        _formModel = new AccountFormModel { Type = AccountType.Checking };
        _showForm = true;
    }

    private void ShowEditForm(AccountDto account)
    {
        _formModel = account.ToForm();
        _showForm = true;
    }

    private void HideForm()
    {
        _showForm = false;
        _formModel = new();
    }

    private async Task HandleSaveAsync(AccountFormModel model)
    {
        model.IsSaving = true;
        model.ErrorMessage = null;

        try
        {
            if (model.Id is null)
            {
                await Api.CreateAccountAsync(model.ToCreateRequest());
                await Toasts.SuccessAsync("Account created.");
            }
            else
            {
                await Api.UpdateAccountAsync(model.Id.Value, model.ToUpdateRequest());
                await Toasts.SuccessAsync("Account updated.");
            }

            _showForm = false;
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
        try
        {
            await Api.DeleteAccountAsync(id);
            await Toasts.SuccessAsync("Account archived.");
            await LoadAsync();
        }
        catch (Exception ex)
        {
            await Toasts.ErrorAsync(ex.Message);
        }
    }
}
