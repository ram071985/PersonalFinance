using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace PersonalFinance.Tests.Integration;

[TestFixture]
public class RecurringIntegrationTests : IntegrationTestBase
{
    [Test]
    public async Task CreateRecurring_ThenList_ReturnsTemplate()
    {
        var auth = await RegisterAsync($"rec_{Guid.NewGuid():N}@test.local");
        using var client = CreateAuthenticatedClient(auth.Token);

        var accountId = await CreateAccountAsync(client, "Checking", balance: 500m);
        var categoryId = await CreateCategoryAsync(client, "Rent", type: 2);

        var day = Math.Min(DateTime.UtcNow.Day, 28);
        var create = await client.PostAsJsonAsync("api/recurring-transactions", new
        {
            accountId,
            categoryId,
            amount = 1200m,
            type = 2, // Expense
            description = "Monthly rent",
            dayOfMonth = day,
            startDate = DateTime.UtcNow.Date.AddMonths(-1),
            isActive = true
        });

        var body = await create.Content.ReadAsStringAsync();
        Assert.That(create.IsSuccessStatusCode, Is.True, body);

        var list = await client.GetFromJsonAsync<JsonElement>("api/recurring-transactions", JsonOptions);
        Assert.That(list.GetArrayLength(), Is.GreaterThanOrEqualTo(1));
    }

    [Test]
    public async Task GenerateDue_OnScheduledDay_CreatesTransactionAndUpdatesBalance()
    {
        var auth = await RegisterAsync($"recgen_{Guid.NewGuid():N}@test.local");
        using var client = CreateAuthenticatedClient(auth.Token);

        var accountId = await CreateAccountAsync(client, "Checking", balance: 2000m);
        var categoryId = await CreateCategoryAsync(client, "Utilities", type: 2);
        var day = Math.Min(DateTime.UtcNow.Day, 28);

        var create = await client.PostAsJsonAsync("api/recurring-transactions", new
        {
            accountId,
            categoryId,
            amount = 75m,
            type = 2,
            description = "Electric",
            dayOfMonth = day,
            startDate = DateTime.UtcNow.Date.AddMonths(-1),
            isActive = true
        });
        Assert.That(create.IsSuccessStatusCode, Is.True, await create.Content.ReadAsStringAsync());
        var created = await create.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var id = created.GetProperty("id").GetInt32();

        var generate = await client.PostAsJsonAsync($"api/recurring-transactions/{id}/generate", new { });
        var genBody = await generate.Content.ReadAsStringAsync();
        Assert.That(generate.IsSuccessStatusCode, Is.True, genBody);

        var account = await client.GetFromJsonAsync<JsonElement>($"api/accounts/{accountId}", JsonOptions);
        Assert.That(account.GetProperty("balance").GetDecimal(), Is.EqualTo(1925m));

        // Second generate same month should fail / not double-charge
        var again = await client.PostAsJsonAsync($"api/recurring-transactions/{id}/generate", new { });
        Assert.That(again.IsSuccessStatusCode, Is.False);

        account = await client.GetFromJsonAsync<JsonElement>($"api/accounts/{accountId}", JsonOptions);
        Assert.That(account.GetProperty("balance").GetDecimal(), Is.EqualTo(1925m));
    }

    [Test]
    public async Task Recurring_IsTenantIsolated()
    {
        var authA = await RegisterAsync($"reca_{Guid.NewGuid():N}@test.local");
        var authB = await RegisterAsync($"recb_{Guid.NewGuid():N}@test.local");
        using var clientA = CreateAuthenticatedClient(authA.Token);
        using var clientB = CreateAuthenticatedClient(authB.Token);

        var accountA = await CreateAccountAsync(clientA, "A", 100m);
        await clientA.PostAsJsonAsync("api/recurring-transactions", new
        {
            accountId = accountA,
            amount = 10m,
            type = 2,
            description = "Only A",
            dayOfMonth = 1,
            startDate = DateTime.UtcNow.Date,
            isActive = true
        });

        var listB = await clientB.GetFromJsonAsync<JsonElement>("api/recurring-transactions", JsonOptions);
        Assert.That(listB.GetArrayLength(), Is.EqualTo(0));
    }

    private async Task<int> CreateAccountAsync(HttpClient client, string name, decimal balance)
    {
        var res = await client.PostAsJsonAsync("api/accounts", new
        {
            name,
            type = 1,
            balance,
            isActive = true
        });
        Assert.That(res.IsSuccessStatusCode, Is.True, await res.Content.ReadAsStringAsync());
        var json = await res.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        return json.GetProperty("id").GetInt32();
    }

    private async Task<int> CreateCategoryAsync(HttpClient client, string name, int type)
    {
        var res = await client.PostAsJsonAsync("api/categories", new
        {
            name,
            type,
            isActive = true
        });
        Assert.That(res.IsSuccessStatusCode, Is.True, await res.Content.ReadAsStringAsync());
        var json = await res.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        return json.GetProperty("id").GetInt32();
    }
}
