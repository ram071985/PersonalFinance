using PersonalFinance.Core.Dtos;
using PersonalFinance.Core.Enums;
using System.Net.Http.Json;
using PersonalFinance.Core.Dtos.Accounts;
using PersonalFinance.Core.Dtos.Budgets;
using PersonalFinance.Core.Dtos.Categories;
using PersonalFinance.Core.Dtos.Dashboard;
using PersonalFinance.Core.Dtos.Transactions;

namespace PersonalFinance.Web.Services;

public class FinanceApiClient
{
    private readonly HttpClient _http;

    public FinanceApiClient(HttpClient http) => _http = http;

    // ── Accounts ──────────────────────────────────────────
    public async Task<List<AccountDto>> GetAccountsAsync() =>
        await _http.GetFromJsonAsync<List<AccountDto>>("api/accounts") ?? new();

    public async Task<AccountDto?> GetAccountAsync(int id) =>
        await _http.GetFromJsonAsync<AccountDto>($"api/accounts/{id}");

    public async Task<AccountDto> CreateAccountAsync(CreateAccountRequest request)
    {
        var response = await _http.PostAsJsonAsync("api/accounts", request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AccountDto>())!;
    }

    public async Task UpdateAccountAsync(int id, UpdateAccountRequest request) =>
        (await _http.PutAsJsonAsync($"api/accounts/{id}", request)).EnsureSuccessStatusCode();

    public async Task DeleteAccountAsync(int id) =>
        (await _http.DeleteAsync($"api/accounts/{id}")).EnsureSuccessStatusCode();

    // ── Categories ────────────────────────────────────────
    public async Task<List<CategoryDto>> GetCategoriesAsync() =>
        await _http.GetFromJsonAsync<List<CategoryDto>>("api/categories") ?? new();

    public async Task<List<CategoryDto>> GetCategoriesByTypeAsync(CategoryType type) =>
        await _http.GetFromJsonAsync<List<CategoryDto>>($"api/categories/type/{type}") ?? new();

    public async Task<CategoryDto> CreateCategoryAsync(CreateCategoryRequest request)
    {
        var response = await _http.PostAsJsonAsync("api/categories", request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CategoryDto>())!;
    }

    public async Task UpdateCategoryAsync(int id, UpdateCategoryRequest request) =>
        (await _http.PutAsJsonAsync($"api/categories/{id}", request)).EnsureSuccessStatusCode();

    public async Task DeleteCategoryAsync(int id) =>
        (await _http.DeleteAsync($"api/categories/{id}")).EnsureSuccessStatusCode();

    // ── Transactions ──────────────────────────────────────
    public async Task<List<TransactionDto>> GetTransactionsAsync() =>
        await _http.GetFromJsonAsync<List<TransactionDto>>("api/transactions") ?? new();

    public async Task<List<TransactionDto>> GetRecentTransactionsAsync(int count = 10) =>
        await _http.GetFromJsonAsync<List<TransactionDto>>($"api/transactions/recent?count={count}") ?? new();

    public async Task<TransactionDto> CreateTransactionAsync(CreateTransactionRequest request)
    {
        var response = await _http.PostAsJsonAsync("api/transactions", request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TransactionDto>())!;
    }

    public async Task UpdateTransactionAsync(int id, UpdateTransactionRequest request) =>
        (await _http.PutAsJsonAsync($"api/transactions/{id}", request)).EnsureSuccessStatusCode();

    public async Task DeleteTransactionAsync(int id) =>
        (await _http.DeleteAsync($"api/transactions/{id}")).EnsureSuccessStatusCode();

    // ── Budgets ───────────────────────────────────────────
    public async Task<List<BudgetDto>> GetBudgetsAsync() =>
        await _http.GetFromJsonAsync<List<BudgetDto>>("api/budgets") ?? new();

    public async Task<List<BudgetDto>> GetBudgetsByMonthAsync(int year, int month) =>
        await _http.GetFromJsonAsync<List<BudgetDto>>($"api/budgets/month/{year}/{month}") ?? new();

    public async Task<BudgetDto> CreateBudgetAsync(CreateBudgetRequest request)
    {
        var response = await _http.PostAsJsonAsync("api/budgets", request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<BudgetDto>())!;
    }

    public async Task UpdateBudgetAsync(int id, UpdateBudgetRequest request) =>
        (await _http.PutAsJsonAsync($"api/budgets/{id}", request)).EnsureSuccessStatusCode();

    public async Task DeleteBudgetAsync(int id) =>
        (await _http.DeleteAsync($"api/budgets/{id}")).EnsureSuccessStatusCode();

    // ── Dashboard ─────────────────────────────────────────
    public async Task<DashboardDto?> GetDashboardAsync() =>
        await _http.GetFromJsonAsync<DashboardDto>("api/dashboard");
}