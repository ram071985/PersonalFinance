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

    protected record AuthResult(string Token, string? RefreshToken, string Email, string UserId);

    protected async Task<AuthResult> RegisterAsync(
        string email,
        string password = "Password1")
    {
        var response = await Client.PostAsJsonAsync("api/auth/register", new { email, password });
        var body = await response.Content.ReadAsStringAsync();
        Assert.That(response.IsSuccessStatusCode, Is.True,
            $"Register failed ({(int)response.StatusCode}): {body}");

        return ParseAuth(body, email);
    }

    protected async Task<AuthResult> LoginAsync(
        string email,
        string password = "Password1")
    {
        var response = await Client.PostAsJsonAsync("api/auth/login", new { email, password });
        var body = await response.Content.ReadAsStringAsync();
        Assert.That(response.IsSuccessStatusCode, Is.True,
            $"Login failed ({(int)response.StatusCode}): {body}");

        return ParseAuth(body, email);
    }

    private static AuthResult ParseAuth(string body, string fallbackEmail)
    {
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        string Read(params string[] names)
        {
            foreach (var n in names)
            {
                if (root.TryGetProperty(n, out var p) && p.ValueKind == JsonValueKind.String)
                    return p.GetString()!;
            }
            return "";
        }

        var token = Read("token", "Token", "accessToken");
        var refresh = root.TryGetProperty("refreshToken", out var rt) ? rt.GetString()
            : root.TryGetProperty("RefreshToken", out var rt2) ? rt2.GetString()
            : null;
        var email = Read("email", "Email");
        if (string.IsNullOrEmpty(email)) email = fallbackEmail;
        var userId = Read("userId", "UserId", "id");

        Assert.That(token, Is.Not.Empty, $"No access token in: {body}");
        return new AuthResult(token, refresh, email, userId);
    }

    protected HttpClient CreateAuthenticatedClient(string token)
    {
        var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}
