using Azure.Identity;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using PersonalFinance.Web.Components;
using PersonalFinance.Web.Services;
using PersonalFinance.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Azure Key Vault when KeyVault:Uri is set (App Service + Managed Identity).
if (!builder.Environment.IsEnvironment("Testing"))
{
    var keyVaultUri = builder.Configuration["KeyVault:Uri"];
    if (!string.IsNullOrWhiteSpace(keyVaultUri))
    {
        builder.Configuration.AddAzureKeyVault(
            new Uri(keyVaultUri),
            new DefaultAzureCredential());
    }
}


// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Needed so AuthorizeRouteView can resolve IAuthenticationService.
// Login state still comes from ServerAuthenticationStateProvider + JWT (circuit-scoped).
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
    });

builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();

// Circuit-scoped
builder.Services.AddScoped<AuthTokenStore>();
builder.Services.AddScoped<ServerAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp =>
    sp.GetRequiredService<ServerAuthenticationStateProvider>());
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<AuthBootstrap>();
builder.Services.AddScoped<ToastService>();
builder.Services.AddScoped<ConfirmService>();
// Empty string in appsettings is not null — treat whitespace as missing.
var apiBase = builder.Configuration["ApiBaseUrl"];
if (string.IsNullOrWhiteSpace(apiBase))
    apiBase = "https://localhost:7000/";
if (!apiBase.EndsWith('/'))
    apiBase += "/";

// Login/register — no bearer
builder.Services.AddHttpClient("AuthApi", c => c.BaseAddress = new Uri(apiBase));

// Named client (no typed client) so FinanceApiClient can be circuit-scoped
// with the same AuthTokenStore instance as login/auth state.
builder.Services.AddHttpClient("FinanceApi", c => c.BaseAddress = new Uri(apiBase));
builder.Services.AddScoped<FinanceApiClient>(sp =>
{
    var http = sp.GetRequiredService<IHttpClientFactory>().CreateClient("FinanceApi");
    var tokens = sp.GetRequiredService<AuthTokenStore>();
    var auth = sp.GetRequiredService<AuthService>();
    return new FinanceApiClient(http, tokens, auth);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
// Azure terminates TLS at the front door; container listens on HTTP :8080 only.
if (!app.Environment.IsProduction())
    app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
