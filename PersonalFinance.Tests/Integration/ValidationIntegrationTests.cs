using System.Net;
using System.Net.Http.Json;

namespace PersonalFinance.Tests.Integration;

[TestFixture]
public class ValidationIntegrationTests : IntegrationTestBase
{
    [Test]
    public async Task CreateTransaction_ZeroAmount_Returns400()
    {
        var (token, _, _) = await RegisterAsync($"val_{Guid.NewGuid():N}@test.local");
        using var client = CreateAuthenticatedClient(token);

        // Need an account first
        var accountRes = await client.PostAsJsonAsync("api/accounts", new
        {
            name = "Checking",
            type = 1,
            balance = 100m,
            isActive = true
        });
        var account = await accountRes.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>(JsonOptions);
        var accountId = account.GetProperty("id").GetInt32();

        var catRes = await client.PostAsJsonAsync("api/categories", new
        {
            name = "Food",
            type = 2,
            isActive = true
        });
        var cat = await catRes.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>(JsonOptions);
        var categoryId = cat.GetProperty("id").GetInt32();

        var response = await client.PostAsJsonAsync("api/transactions", new
        {
            accountId,
            categoryId,
            amount = 0m,
            type = 2, // Expense
            description = "Bad",
            date = DateTime.UtcNow.Date
        });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task CreateAccount_EmptyName_Returns400()
    {
        var (token, _, _) = await RegisterAsync($"val2_{Guid.NewGuid():N}@test.local");
        using var client = CreateAuthenticatedClient(token);

        var response = await client.PostAsJsonAsync("api/accounts", new
        {
            name = "",
            type = 1,
            balance = 0m,
            isActive = true
        });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }
}
