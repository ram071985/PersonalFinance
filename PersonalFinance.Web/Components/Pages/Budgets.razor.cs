using Microsoft.AspNetCore.Components;
using PersonalFinance.Core.Dtos.Budgets;
using PersonalFinance.Core.Dtos.Categories;
using PersonalFinance.Core.Enums;
using PersonalFinance.Models;
using PersonalFinance.Web.Services;

namespace PersonalFinance.Components.Pages;

public partial class Budgets : ComponentBase
{
    private List<BudgetDto> _budgets = new();
    private List<CategoryDto> _expenseCategories = new();
    private BudgetFormModel _formModel = new();
    [Inject]
    private FinanceApiClient Api { get; set; } = default!;
    private bool _isLoading = true;
    private bool _showForm;
    private int _selectedYear = DateTime.Now.Year;
    private int _selectedMonth = DateTime.Now.Month;

    protected override async Task OnInitializedAsync() => await LoadAsync();

    private async Task LoadAsync()
    {
        _isLoading = true;
        try
        {
            _budgets = await Api.GetBudgetsByMonthAsync(_selectedYear, _selectedMonth);
            _expenseCategories = await Api.GetCategoriesByTypeAsync(CategoryType.Expense);
        }
        catch
        {
            _budgets = new();
            _expenseCategories = new();
        }
        _isLoading = false;
    }

    private void ShowCreateForm()
    {
        _formModel = new BudgetFormModel
        {
            Year = _selectedYear,
            Month = _selectedMonth,
            CategoryId = _expenseCategories.FirstOrDefault()?.Id ?? 0
        };
        _showForm = true;
    }

    private void ShowEditForm(BudgetDto b)
    {
        _formModel = b.ToForm();
        _showForm = true;
    }

    private void HideForm()
    {
        _showForm = false;
        _formModel = new();
    }

    private async Task HandleSaveAsync(BudgetFormModel model)
    {
        model.IsSaving = true;
        model.ErrorMessage = null;
        try
        {
            if (model.Id is null)
                await Api.CreateBudgetAsync(model.ToCreateRequest());
            else
                await Api.UpdateBudgetAsync(model.Id.Value, model.ToUpdateRequest());

            _showForm = false;
            await LoadAsync();
        }
        catch (Exception ex) { model.ErrorMessage = ex.Message; }
        finally { model.IsSaving = false; }
    }

    private async Task DeleteAsync(int id)
    {
        await Api.DeleteBudgetAsync(id);
        await LoadAsync();
    }
}