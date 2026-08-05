using System.Net;
using System.Net.Http.Json;

namespace PersonalFinance.Tests.Integration;

[TestFixture]
public class AuthIntegrationTests : IntegrationTestBase
{
    [Test]
    public async Task Register_ReturnsJwt()
    {
        var email = $"user_{Guid.NewGuid():N}@test.local";
        var auth = await RegisterAsync(email);

        Assert.That(auth.Token, Is.Not.Empty);
        Assert.That(auth.UserId, Is.Not.Empty);
    }

    [Test]
    public async Task Login_WithValidCredentials_ReturnsJwt()
    {
        var email = $"login_{Guid.NewGuid():N}@test.local";
        await RegisterAsync(email);

        var auth = await LoginAsync(email);

        Assert.That(auth.Token, Is.Not.Empty);
        Assert.That(auth.Email, Is.EqualTo(email).IgnoreCase);
    }

    [Test]
    public async Task Login_WithBadPassword_Returns401()
    {
        var email = $"bad_{Guid.NewGuid():N}@test.local";
        await RegisterAsync(email);

        var response = await Client.PostAsJsonAsync(
            "api/auth/login",
            new { email, password = "WrongPassword1" });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task FinanceEndpoint_WithoutToken_Returns401()
    {
        var response = await Client.GetAsync("api/accounts");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task FinanceEndpoint_WithToken_Returns200()
    {
        var email = $"authz_{Guid.NewGuid():N}@test.local";
        var auth = await RegisterAsync(email);

        using var client = CreateAuthenticatedClient(auth.Token);
        var response = await client.GetAsync("api/accounts");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }
}