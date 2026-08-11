using Microsoft.AspNetCore.Components;
using PersonalFinance.Core.Dtos.Categories;
using PersonalFinance.Core.Enums;
using PersonalFinance.Web.Models;
using PersonalFinance.Web.Services;

namespace PersonalFinance.Web.Components.Pages;

public partial class Categories : ComponentBase
{
    private List<CategoryDto> _categories = new();
    private CategoryFormModel _formModel = new();
    [Inject]
    private FinanceApiClient Api { get; set; } = default!;
    [Inject] private ToastService Toasts { get; set; } = default!;
    [Inject] private ConfirmService Confirm { get; set; } = default!;
    private bool _isLoading = true;
    private bool _showForm;

    protected override async Task OnInitializedAsync() => await LoadAsync();

    private async Task LoadAsync()
    {
        _isLoading = true;
        try { _categories = await Api.GetCategoriesAsync(); }
        catch (Exception ex)
        {
            _categories = new();
            await Toasts.ErrorAsync($"Could not load categories: {ex.Message}");
        }
        _isLoading = false;
    }

    private void ShowCreateForm()
    {
        _formModel = new CategoryFormModel { Type = CategoryType.Expense };
        _showForm = true;
    }

    private void ShowEditForm(CategoryDto c)
    {
        _formModel = c.ToForm();
        _showForm = true;
    }

    private void HideForm()
    {
        _showForm = false;
        _formModel = new();
    }

    private async Task HandleSaveAsync(CategoryFormModel model)
    {
        model.IsSaving = true;
        model.ErrorMessage = null;
        try
        {
            if (model.Id is null)
            {
                await Api.CreateCategoryAsync(model.ToCreateRequest());
                await Toasts.SuccessAsync("Category created.");
            }
            else
            {
                await Api.UpdateCategoryAsync(model.Id.Value, model.ToUpdateRequest());
                await Toasts.SuccessAsync("Category updated.");
            }

            _showForm = false;
            await LoadAsync();
        }
        catch (Exception ex)
        {
            model.ErrorMessage = ex.Message;
            await Toasts.ErrorAsync(ex.Message);
        }
        finally { model.IsSaving = false; }
    }

    private async Task DeleteAsync(int id)
    {
        if (!await Confirm.ShowAsync(
                "Delete this category? Transactions keep their history but lose this category link if the API clears it.",
                title: "Delete category",
                confirmText: "Delete"))
            return;

        try
        {
            await Api.DeleteCategoryAsync(id);
            await Toasts.SuccessAsync("Category deleted.");
            await LoadAsync();
        }
        catch (Exception ex)
        {
            await Toasts.ErrorAsync(ex.Message);
        }
    }
}