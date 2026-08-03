using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace PersonalFinance.Tests.Integration;

[TestFixture]
public class TenancyIntegrationTests : IntegrationTestBase
{
    [Test]
    public async Task UserB_CannotRead_UserA_Account()
    {
        var (tokenA, _, _, _) = await RegisterAsync($"a_{Guid.NewGuid():N}@test.local");
        var (tokenB, _, _, _) = await RegisterAsync($"b_{Guid.NewGuid():N}@test.local");

        using var clientA = CreateAuthenticatedClient(tokenA);
        using var clientB = CreateAuthenticatedClient(tokenB);

        // User A creates an account
        var create = await clientA.PostAsJsonAsync("api/accounts", new
        {
            name = "A Checking",
            type = 1,
            balance = 100m,
            isActive = true
        });
        Assert.That(create.IsSuccessStatusCode, Is.True, await create.Content.ReadAsStringAsync());

        var created = await create.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var accountId = created.GetProperty("id").GetInt32();

        // User B cannot get it by id
        var get = await clientB.GetAsync($"api/accounts/{accountId}");
        Assert.That(get.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));

        // User B list does not contain A's account
        var list = await clientB.GetFromJsonAsync<List<JsonElement>>("api/accounts", JsonOptions);
        Assert.That(list!.Any(a => a.GetProperty("id").GetInt32() == accountId), Is.False);
    }

    [Test]
    public async Task UserB_CannotUpdate_UserA_Account()
    {
        var (tokenA, _, _, _) = await RegisterAsync($"a2_{Guid.NewGuid():N}@test.local");
        var (tokenB, _, _, _) = await RegisterAsync($"b2_{Guid.NewGuid():N}@test.local");

        using var clientA = CreateAuthenticatedClient(tokenA);
        using var clientB = CreateAuthenticatedClient(tokenB);

        var create = await clientA.PostAsJsonAsync("api/accounts", new
        {
            name = "Private",
            type = 1,
            balance = 50m,
            isActive = true
        });
        var created = await create.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var accountId = created.GetProperty("id").GetInt32();

        var update = await clientB.PutAsJsonAsync($"api/accounts/{accountId}", new
        {
            name = "Hacked",
            type = 1,
            balance = 0m,
            isActive = true
        });

        Assert.That(update.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }
}
