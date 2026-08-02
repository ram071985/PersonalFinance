using System.Net.Http.Headers;
using System.Net.Http.Json;
using PersonalFinance.Core.Dtos.Accounts;
using PersonalFinance.Core.Dtos.Budgets;
using PersonalFinance.Core.Dtos.Categories;
using PersonalFinance.Core.Dtos.Dashboard;
using PersonalFinance.Core.Dtos.Transactions;
using PersonalFinance.Core.Enums;
using PersonalFinance.Services;

namespace PersonalFinance.Web.Services;

/// <summary>
/// Typed API client. Attaches JWT from the circuit-scoped AuthTokenStore on every request.
/// Blazor Server: DelegatingHandler cannot see the circuit's AuthTokenStore (different DI scope).
/// </summary>
public class FinanceApiClient
{
    private readonly HttpClient _http;
    private readonly AuthTokenStore _tokens;

    public FinanceApiClient(HttpClient http, AuthTokenStore tokens)
    {
        _http = http;
        _tokens = tokens;
    }

    private void ApplyBearer(HttpRequestMessage request)
    {
        if (_tokens.IsAuthenticated && !string.IsNullOrWhiteSpace(_tokens.AccessToken))
        {
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", _tokens.AccessToken);
        }
    }

    private async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request)
    {
        await _tokens.EnsureRestoredAsync();
        ApplyBearer(request);
        return await _http.SendAsync(request);
    }

    private async Task EnsureSuccess(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode) return;

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            throw new HttpRequestException("Unauthorized (401). Log in again — JWT missing or expired.");

        var body = await response.Content.ReadAsStringAsync();
        throw new HttpRequestException($"API {(int)response.StatusCode}: {body}");
    }

    private async Task<T?> GetJsonAsync<T>(string url)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        var response = await SendAsync(request);
        await EnsureSuccess(response);
        return await response.Content.ReadFromJsonAsync<T>();
    }

    private async Task<T> PostJsonAsync<T>(string url, object body)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(body)
        };
        var response = await SendAsync(request);
        await EnsureSuccess(response);
        return (await response.Content.ReadFromJsonAsync<T>())!;
    }

    private async Task PutJsonAsync(string url, object body)
    {
        using var request = new HttpRequestMessage(HttpMethod.Put, url)
        {
            Content = JsonContent.Create(body)
        };
        var response = await SendAsync(request);
        await EnsureSuccess(response);
    }

    private async Task DeleteAsync(string url)
    {
        using var request = new HttpRequestMessage(HttpMethod.Delete, url);
        var response = await SendAsync(request);
        await EnsureSuccess(response);
    }

    // ── Accounts ──────────────────────────────────────────
    public async Task<List<AccountDto>> GetAccountsAsync() =>
        await GetJsonAsync<List<AccountDto>>("api/accounts") ?? new();

    public Task<AccountDto?> GetAccountAsync(int id) =>
        GetJsonAsync<AccountDto>($"api/accounts/{id}");

    public Task<AccountDto> CreateAccountAsync(CreateAccountRequest request) =>
        PostJsonAsync<AccountDto>("api/accounts", request);

    public Task UpdateAccountAsync(int id, UpdateAccountRequest request) =>
        PutJsonAsync($"api/accounts/{id}", request);

    public Task DeleteAccountAsync(int id) =>
        DeleteAsync($"api/accounts/{id}");

    // ── Categories ────────────────────────────────────────
    public async Task<List<CategoryDto>> GetCategoriesAsync() =>
        await GetJsonAsync<List<CategoryDto>>("api/categories") ?? new();

    public async Task<List<CategoryDto>> GetCategoriesByTypeAsync(CategoryType type) =>
        await GetJsonAsync<List<CategoryDto>>($"api/categories/type/{type}") ?? new();

    public Task<CategoryDto> CreateCategoryAsync(CreateCategoryRequest request) =>
        PostJsonAsync<CategoryDto>("api/categories", request);

    public Task UpdateCategoryAsync(int id, UpdateCategoryRequest request) =>
        PutJsonAsync($"api/categories/{id}", request);

    public Task DeleteCategoryAsync(int id) =>
        DeleteAsync($"api/categories/{id}");

    // ── Transactions ──────────────────────────────────────
    public async Task<List<TransactionDto>> GetTransactionsAsync() =>
        await GetJsonAsync<List<TransactionDto>>("api/transactions") ?? new();

    public async Task<List<TransactionDto>> GetRecentTransactionsAsync(int count = 10) =>
        await GetJsonAsync<List<TransactionDto>>($"api/transactions/recent?count={count}") ?? new();

    public Task<TransactionDto> CreateTransactionAsync(CreateTransactionRequest request) =>
        PostJsonAsync<TransactionDto>("api/transactions", request);

    public Task UpdateTransactionAsync(int id, UpdateTransactionRequest request) =>
        PutJsonAsync($"api/transactions/{id}", request);

    public Task DeleteTransactionAsync(int id) =>
        DeleteAsync($"api/transactions/{id}");

    // ── Budgets ───────────────────────────────────────────
    public async Task<List<BudgetDto>> GetBudgetsAsync() =>
        await GetJsonAsync<List<BudgetDto>>("api/budgets") ?? new();

    public async Task<List<BudgetDto>> GetBudgetsByMonthAsync(int year, int month) =>
        await GetJsonAsync<List<BudgetDto>>($"api/budgets/month/{year}/{month}") ?? new();

    public Task<BudgetDto> CreateBudgetAsync(CreateBudgetRequest request) =>
        PostJsonAsync<BudgetDto>("api/budgets", request);

    public Task UpdateBudgetAsync(int id, UpdateBudgetRequest request) =>
        PutJsonAsync($"api/budgets/{id}", request);

    public Task DeleteBudgetAsync(int id) =>
        DeleteAsync($"api/budgets/{id}");

    // ── Dashboard ─────────────────────────────────────────
    public Task<DashboardDto?> GetDashboardAsync() =>
        GetJsonAsync<DashboardDto>("api/dashboard");
}
