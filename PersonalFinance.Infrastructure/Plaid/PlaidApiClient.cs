using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace PersonalFinance.Infrastructure.Plaid;

public class PlaidApiClient
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly HttpClient _http;
    private readonly PlaidOptions _options;
    private readonly ILogger<PlaidApiClient> _logger;

    public PlaidApiClient(HttpClient http, IOptions<PlaidOptions> options, ILogger<PlaidApiClient> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
        _http.BaseAddress = new Uri(_options.BaseUrl.TrimEnd('/') + "/");
    }

    private object Creds() => new { client_id = _options.ClientId, secret = _options.Secret };

    public async Task<JsonElement> PostAsync(string path, object body, CancellationToken ct = default)
    {
        // Merge credentials into body via dictionary
        var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(
            JsonSerializer.Serialize(body, JsonOpts)) ?? new();
        dict["client_id"] = JsonSerializer.SerializeToElement(_options.ClientId);
        dict["secret"] = JsonSerializer.SerializeToElement(_options.Secret);

        using var response = await _http.PostAsJsonAsync(path.TrimStart('/'), dict, JsonOpts, ct);
        var text = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Plaid {Path} failed ({Status}): {Error}", path, (int)response.StatusCode, TrimError(text));
            throw new InvalidOperationException($"Plaid error ({(int)response.StatusCode}): {TrimError(text)}");
        }

        return JsonSerializer.Deserialize<JsonElement>(text);
    }

    private static string TrimError(string text)
    {
        try
        {
            using var doc = JsonDocument.Parse(text);
            if (doc.RootElement.TryGetProperty("error_message", out var m))
                return m.GetString() ?? text;
        }
        catch { /* ignore */ }
        return text.Length > 300 ? text[..300] : text;
    }
}
