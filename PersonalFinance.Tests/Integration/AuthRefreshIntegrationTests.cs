using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace PersonalFinance.Tests.Integration;

[TestFixture]
public class AuthRefreshIntegrationTests : IntegrationTestBase
{
    [Test]
    public async Task Register_SetsRefreshCookie_AndAccessToken()
    {
        var email = $"refresh_{Guid.NewGuid():N}@test.local";
        var (auth, response) = await RegisterWithResponseAsync(email);

        Assert.That(auth.Token, Is.Not.Empty);
        Assert.That(
            HasRefreshCookie(response),
            Is.True,
            "Expected httpOnly pf_refresh cookie on register response.");
        // Body no longer carries refresh token (cookie only)
        Assert.That(auth.RefreshToken, Is.Null.Or.Empty);
    }

    [Test]
    public async Task Refresh_WithCookie_ReturnsNewAccessToken()
    {
        var email = $"refresh2_{Guid.NewGuid():N}@test.local";
        var (auth, _) = await RegisterWithResponseAsync(email);

        // Same client keeps cookie jar from register
        var response = await Client.PostAsJsonAsync("api/auth/refresh", new { });
        var body = await response.Content.ReadAsStringAsync();
        Assert.That(response.IsSuccessStatusCode, Is.True, body);

        using var doc = JsonDocument.Parse(body);
        var newToken = doc.RootElement.GetProperty("token").GetString();
        Assert.That(newToken, Is.Not.Null.And.Not.Empty);
        Assert.That(newToken, Is.Not.EqualTo(auth.Token));

        using var client = CreateAuthenticatedClient(newToken!);
        var accounts = await client.GetAsync("api/accounts");
        Assert.That(accounts.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task Refresh_WithInvalidToken_Returns401()
    {
        var response = await Client.PostAsJsonAsync(
            "api/auth/refresh",
            new { refreshToken = "not-a-real-refresh-token" });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task Logout_ClearsCookie_AndInvalidatesRefresh()
    {
        var email = $"logout_{Guid.NewGuid():N}@test.local";
        var (auth, _) = await RegisterWithResponseAsync(email);

        using var client = CreateAuthenticatedClient(auth.Token);
        // Copy cookies so logout/refresh share jar with register
        var logout = await Client.PostAsync("api/auth/logout", null);
        Assert.That(logout.StatusCode, Is.EqualTo(HttpStatusCode.NoContent)
            .Or.EqualTo(HttpStatusCode.OK));

        var refresh = await Client.PostAsJsonAsync("api/auth/refresh", new { });
        Assert.That(refresh.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    private static bool HasRefreshCookie(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var values))
            return false;
        return values.Any(v =>
            v.StartsWith("pf_refresh=", StringComparison.OrdinalIgnoreCase));
    }
}
