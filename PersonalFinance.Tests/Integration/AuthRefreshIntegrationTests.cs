using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace PersonalFinance.Tests.Integration;

[TestFixture]
public class AuthRefreshIntegrationTests : IntegrationTestBase
{
    [Test]
    public async Task Register_ReturnsRefreshToken()
    {
        var email = $"refresh_{Guid.NewGuid():N}@test.local";
        var auth = await RegisterAsync(email);

        Assert.That(auth.Token, Is.Not.Empty);
        Assert.That(auth.RefreshToken, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public async Task Refresh_WithValidToken_ReturnsNewAccessToken()
    {
        var email = $"refresh2_{Guid.NewGuid():N}@test.local";
        var auth = await RegisterAsync(email);

        var response = await Client.PostAsJsonAsync(
            "api/auth/refresh",
            new { refreshToken = auth.RefreshToken });

        var body = await response.Content.ReadAsStringAsync();
        Assert.That(response.IsSuccessStatusCode, Is.True, body);

        using var doc = JsonDocument.Parse(body);
        var newToken = doc.RootElement.GetProperty("token").GetString();
        Assert.That(newToken, Is.Not.Null.And.Not.Empty);

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
    public async Task Logout_InvalidatesRefreshToken()
    {
        var email = $"logout_{Guid.NewGuid():N}@test.local";
        var auth = await RegisterAsync(email);

        using var client = CreateAuthenticatedClient(auth.Token);
        var logout = await client.PostAsync("api/auth/logout", null);
        Assert.That(logout.StatusCode, Is.EqualTo(HttpStatusCode.NoContent)
            .Or.EqualTo(HttpStatusCode.OK));

        var refresh = await Client.PostAsJsonAsync(
            "api/auth/refresh",
            new { refreshToken = auth.RefreshToken });

        Assert.That(refresh.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }
}
