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
    private bool _isLoading = true;
    private bool _showForm;

    protected override async Task OnInitializedAsync() => await LoadAsync();

    private async Task LoadAsync()
    {
        _isLoading = true;
        try { _categories = await Api.GetCategoriesAsync(); }
        catch { _categories = new(); }
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
                await Api.CreateCategoryAsync(model.ToCreateRequest());
            else
                await Api.UpdateCategoryAsync(model.Id.Value, model.ToUpdateRequest());

            _showForm = false;
            await LoadAsync();
        }
        catch (Exception ex) { model.ErrorMessage = ex.Message; }
        finally { model.IsSaving = false; }
    }

    private async Task DeleteAsync(int id)
    {
        await Api.DeleteCategoryAsync(id);
        await LoadAsync();
    }
}