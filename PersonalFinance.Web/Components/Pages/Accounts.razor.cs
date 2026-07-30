using Microsoft.AspNetCore.Components;
using PersonalFinance.Core.Dtos.Accounts;
using PersonalFinance.Core.Enums;
using PersonalFinance.Models;
using PersonalFinance.Web.Models;

namespace PersonalFinance.Components.Pages;

public partial class Accounts : ComponentBase
{
    private List<AccountDto> _accounts = new();
    private AccountFormModel _formModel = new();
    private bool _isLoading = true;
    private bool _showForm;

    protected override async Task OnInitializedAsync() => await LoadAsync();

    private async Task LoadAsync()
    {
        _isLoading = true;
        try
        {
            _accounts = await Api.GetAccountsAsync();
        }
        catch
        {
            _accounts = new();
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
        _formModel = account.ToForm();   // existing FormMappings
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
                await Api.CreateAccountAsync(model.ToCreateRequest());
            else
                await Api.UpdateAccountAsync(model.Id.Value, model.ToUpdateRequest());

            _showForm = false;
            await LoadAsync();
        }
        catch (Exception ex)
        {
            model.ErrorMessage = ex.Message;
        }
        finally
        {
            model.IsSaving = false;
        }
    }

    private async Task DeleteAsync(int id)
    {
        await Api.DeleteAccountAsync(id);
        await LoadAsync();
    }
}