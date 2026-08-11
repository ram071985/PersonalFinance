using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using PersonalFinance.Infrastructure.Data;

namespace PersonalFinance.Tests.Integration;

[TestFixture]
public class MoneyIntegrationTests : IntegrationTestBase
{
    [Test]
    public async Task CreateExpense_DecreasesAccountBalance()
    {
        var auth = await RegisterAsync($"money_{Guid.NewGuid():N}@test.local");
        using var client = CreateAuthenticatedClient(auth.Token);

        var accountRes = await client.PostAsJsonAsync("api/accounts", new
        {
            name = "Checking",
            type = 1,
            balance = 200m,
            isActive = true
        });
        Assert.That(accountRes.IsSuccessStatusCode, Is.True, await accountRes.Content.ReadAsStringAsync());
        var account = await accountRes.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var accountId = account.GetProperty("id").GetInt32();

        var catRes = await client.PostAsJsonAsync("api/categories", new
        {
            name = "Groceries",
            type = 2,
            isActive = true
        });
        var cat = await catRes.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var categoryId = cat.GetProperty("id").GetInt32();

        var txRes = await client.PostAsJsonAsync("api/transactions", new
        {
            accountId,
            categoryId,
            amount = 35.50m,
            type = 2, // Expense
            description = "Market",
            date = DateTime.UtcNow.Date
        });
        Assert.That(txRes.IsSuccessStatusCode, Is.True, await txRes.Content.ReadAsStringAsync());

        var updated = await client.GetFromJsonAsync<JsonElement>($"api/accounts/{accountId}", JsonOptions);
        var balance = updated.GetProperty("balance").GetDecimal();
        Assert.That(balance, Is.EqualTo(164.50m));
    }

    [Test]
    public async Task CreateIncome_IncreasesAccountBalance()
    {
        var auth = await RegisterAsync($"income_{Guid.NewGuid():N}@test.local");
        using var client = CreateAuthenticatedClient(auth.Token);

        var accountRes = await client.PostAsJsonAsync("api/accounts", new
        {
            name = "Savings",
            type = 2,
            balance = 100m,
            isActive = true
        });
        var account = await accountRes.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var accountId = account.GetProperty("id").GetInt32();

        var catRes = await client.PostAsJsonAsync("api/categories", new
        {
            name = "Salary",
            type = 1, // Income category
            isActive = true
        });
        var cat = await catRes.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var categoryId = cat.GetProperty("id").GetInt32();

        var txRes = await client.PostAsJsonAsync("api/transactions", new
        {
            accountId,
            categoryId,
            amount = 50m,
            type = 1, // Income
            description = "Pay",
            date = DateTime.UtcNow.Date
        });
        Assert.That(txRes.IsSuccessStatusCode, Is.True, await txRes.Content.ReadAsStringAsync());

        var updated = await client.GetFromJsonAsync<JsonElement>($"api/accounts/{accountId}", JsonOptions);
        Assert.That(updated.GetProperty("balance").GetDecimal(), Is.EqualTo(150m));
    }

    [Test]
    public async Task DeleteTransaction_ReversesBalance()
    {
        var auth = await RegisterAsync($"del_{Guid.NewGuid():N}@test.local");
        using var client = CreateAuthenticatedClient(auth.Token);

        var accountRes = await client.PostAsJsonAsync("api/accounts", new
        {
            name = "Checking",
            type = 1,
            balance = 100m,
            isActive = true
        });
        var account = await accountRes.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var accountId = account.GetProperty("id").GetInt32();

        var catRes = await client.PostAsJsonAsync("api/categories", new
        {
            name = "Food",
            type = 2,
            isActive = true
        });
        var cat = await catRes.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var categoryId = cat.GetProperty("id").GetInt32();

        var txRes = await client.PostAsJsonAsync("api/transactions", new
        {
            accountId,
            categoryId,
            amount = 25m,
            type = 2,
            description = "Lunch",
            date = DateTime.UtcNow.Date
        });
        var tx = await txRes.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var txId = tx.GetProperty("id").GetInt32();

        var del = await client.DeleteAsync($"api/transactions/{txId}");
        Assert.That(del.IsSuccessStatusCode, Is.True);

        var updated = await client.GetFromJsonAsync<JsonElement>($"api/accounts/{accountId}", JsonOptions);
        Assert.That(updated.GetProperty("balance").GetDecimal(), Is.EqualTo(100m));
    }

    [Test]
    public async Task Dashboard_ReturnsSummary_ForCurrentUser()
    {
        var auth = await RegisterAsync($"dash_{Guid.NewGuid():N}@test.local");
        using var client = CreateAuthenticatedClient(auth.Token);

        var response = await client.GetAsync("api/dashboard");
        Assert.That(response.IsSuccessStatusCode, Is.True, await response.Content.ReadAsStringAsync());

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        Assert.That(json.TryGetProperty("netWorth", out _) || json.TryGetProperty("NetWorth", out _), Is.True);
    }
}
