using System.Net.Http.Headers;
using System.Net.Http.Json;
using PersonalFinance.Core.Dtos.Accounts;
using PersonalFinance.Core.Dtos.Budgets;
using PersonalFinance.Core.Dtos.Categories;
using PersonalFinance.Core.Dtos.Dashboard;
using PersonalFinance.Core.Dtos.Transactions;
using PersonalFinance.Core.Dtos.Recurring;
using PersonalFinance.Core.Dtos.Notifications;
using PersonalFinance.Core.Enums;
using PersonalFinance.Web.Services;

namespace PersonalFinance.Web.Services;

/// <summary>
/// Typed API client. Attaches JWT from the circuit-scoped AuthTokenStore on every request.
/// Blazor Server: DelegatingHandler cannot see the circuit's AuthTokenStore (different DI scope).
/// </summary>
public class FinanceApiClient
{
    private readonly HttpClient _http;
    private readonly AuthTokenStore _tokens;
    private readonly AuthService _auth;

    public FinanceApiClient(HttpClient http, AuthTokenStore tokens, AuthService auth)
    {
        _http = http;
        _tokens = tokens;
        _auth = auth;
    }

    private void ApplyBearer(HttpRequestMessage request)
    {
        if (_tokens.IsAuthenticated && !string.IsNullOrWhiteSpace(_tokens.AccessToken))
        {
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", _tokens.AccessToken);
        }
    }

    private Task<HttpResponseMessage> SendAsync(HttpRequestMessage request) =>
        SendWithRefreshAsync(request);

    private async Task EnsureSuccess(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode) return;

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            throw new HttpRequestException("Unauthorized (401). Log in again — JWT missing or expired.");

        var body = await response.Content.ReadAsStringAsync();
        throw new HttpRequestException($"API {(int)response.StatusCode}: {body}");
    }

    private async Task<HttpResponseMessage> SendWithRefreshAsync(HttpRequestMessage request)
    {
        await _tokens.EnsureRestoredAsync();
        if (_tokens.AccessTokenExpiringSoon || string.IsNullOrWhiteSpace(_tokens.AccessToken))
            await _auth.TryRefreshAsync();

        ApplyBearer(request);
        var response = await _http.SendAsync(request);

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            // Refresh token is httpOnly cookie — browser fetch via AuthService
            var refreshed = await _auth.TryRefreshAsync();
            if (refreshed)
            {
                // retry once with new access token
                var retry = new HttpRequestMessage(request.Method, request.RequestUri);
                if (request.Content is not null)
                {
                    var bytes = await request.Content.ReadAsByteArrayAsync();
                    retry.Content = new ByteArrayContent(bytes);
                    foreach (var h in request.Content.Headers)
                        retry.Content.Headers.TryAddWithoutValidation(h.Key, h.Value);
                }
                ApplyBearer(retry);
                response.Dispose();
                response = await _http.SendAsync(retry);
            }
        }

        return response;
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

    private async Task PostEmptyAsync(string url)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, url);
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

    public async Task<BankStatementImportResult> ImportBankStatementAsync(
        int accountId,
        int? expenseCategoryId,
        int? incomeCategoryId,
        Stream fileStream,
        string fileName)
    {
        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(accountId.ToString()), "accountId");
        if (expenseCategoryId is > 0)
            content.Add(new StringContent(expenseCategoryId.Value.ToString()), "expenseCategoryId");
        if (incomeCategoryId is > 0)
            content.Add(new StringContent(incomeCategoryId.Value.ToString()), "incomeCategoryId");

        var streamContent = new StreamContent(fileStream);
        streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/csv");
        content.Add(streamContent, "file", fileName);

        using var request = new HttpRequestMessage(HttpMethod.Post, "api/transactions/import/csv")
        {
            Content = content
        };
        var response = await SendAsync(request);
        await EnsureSuccess(response);
        return (await response.Content.ReadFromJsonAsync<BankStatementImportResult>())!;
    }

    public async Task<byte[]> ExportTransactionsCsvAsync()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "api/transactions/export/csv");
        var response = await SendAsync(request);
        await EnsureSuccess(response);
        return await response.Content.ReadAsByteArrayAsync();
    }

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

    // ── Recurring ─────────────────────────────────────────
    public async Task<List<RecurringTransactionDto>> GetRecurringAsync() =>
        await GetJsonAsync<List<RecurringTransactionDto>>("api/recurring-transactions") ?? new();

    public Task<RecurringTransactionDto> CreateRecurringAsync(CreateRecurringTransactionRequest request) =>
        PostJsonAsync<RecurringTransactionDto>("api/recurring-transactions", request);

    public Task DeleteRecurringAsync(int id) =>
        DeleteAsync($"api/recurring-transactions/{id}");

    public Task<TransactionDto> GenerateRecurringAsync(int id) =>
        PostJsonAsync<TransactionDto>($"api/recurring-transactions/{id}/generate", new { });

    // ── Notifications ─────────────────────────────────────
    public async Task<List<NotificationDto>> GetNotificationsAsync(int take = 20) =>
        await GetJsonAsync<List<NotificationDto>>($"api/notifications?take={take}") ?? new();

    public async Task<int> GetUnreadNotificationCountAsync()
    {
        var el = await GetJsonAsync<System.Text.Json.JsonElement>("api/notifications/unread-count");
        if (el.ValueKind == System.Text.Json.JsonValueKind.Object && el.TryGetProperty("count", out var c))
            return c.GetInt32();
        return 0;
    }

    public Task MarkNotificationReadAsync(int id) =>
        PostEmptyAsync($"api/notifications/{id}/read");

    public Task MarkAllNotificationsReadAsync() =>
        PostEmptyAsync("api/notifications/read-all");

    // ── Dashboard ─────────────────────────────────────────
    public Task<DashboardDto?> GetDashboardAsync() =>
        GetJsonAsync<DashboardDto>("api/dashboard");
}
