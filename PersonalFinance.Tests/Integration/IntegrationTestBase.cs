using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace PersonalFinance.Tests.Integration;

public abstract class IntegrationTestBase
{
    protected static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    protected ApiFactory Factory { get; private set; } = null!;
    protected HttpClient Client { get; private set; } = null!;

    [OneTimeSetUp]
    public async Task OneTimeSetUpAsync()
    {
        Factory = new ApiFactory();
        Client = Factory.CreateClient();

        try
        {
            await Factory.ResetDatabaseAsync();
        }
        catch (Exception ex)
        {
            Assert.Fail(
                "Could not connect to SQL Server test database.\n" +
                $"Connection: {Factory.ConnectionString}\n" +
                "Set env TEST_CONNECTION_STRING to a reachable SQL Server database.\n" +
                $"Error: {ex.Message}");
        }
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        Client?.Dispose();
        Factory?.Dispose();
    }

    protected async Task<(string Token, string Email, string UserId)> RegisterAsync(
        string email,
        string password = "Password1")
    {
        var response = await Client.PostAsJsonAsync("api/auth/register", new { email, password });
        var body = await response.Content.ReadAsStringAsync();
        Assert.That(response.IsSuccessStatusCode, Is.True,
            $"Register failed ({(int)response.StatusCode}): {body}");

        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;
        var token = root.GetProperty("token").GetString()!;
        var userId = root.GetProperty("userId").GetString()!;
        return (token, email, userId);
    }

    protected async Task<(string Token, string Email, string UserId)> LoginAsync(
        string email,
        string password = "Password1")
    {
        var response = await Client.PostAsJsonAsync("api/auth/login", new { email, password });
        var body = await response.Content.ReadAsStringAsync();
        Assert.That(response.IsSuccessStatusCode, Is.True,
            $"Login failed ({(int)response.StatusCode}): {body}");

        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;
        return (
            root.GetProperty("token").GetString()!,
            root.GetProperty("email").GetString()!,
            root.GetProperty("userId").GetString()!);
    }

    protected HttpClient CreateAuthenticatedClient(string token)
    {
        var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}