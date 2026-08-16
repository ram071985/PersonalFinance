using Azure.Identity;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using PersonalFinance.Web.Components;
using PersonalFinance.Web.Services;

var builder = WebApplication.CreateBuilder(args);

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

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Cookie scheme only so IAuthenticationService resolves for AuthorizeRouteView.
// Real sign-in state is JWT in sessionStorage via ServerAuthenticationStateProvider.
// Do NOT call UseAuthentication/UseAuthorization — that causes
// /login?ReturnUrl=%2F redirects with no auth cookie.
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.Events.OnRedirectToLogin = ctx =>
        {
            ctx.Response.StatusCode = 401;
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = ctx =>
        {
            ctx.Response.StatusCode = 403;
            return Task.CompletedTask;
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();

builder.Services.AddScoped<AuthTokenStore>();
builder.Services.AddScoped<ServerAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp =>
    sp.GetRequiredService<ServerAuthenticationStateProvider>());
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<ToastService>();
builder.Services.AddScoped<ConfirmService>();

var apiBase = builder.Configuration["ApiBaseUrl"];
if (string.IsNullOrWhiteSpace(apiBase))
    apiBase = "https://localhost:7000/";
if (!apiBase.EndsWith('/'))
    apiBase += "/";

builder.Services.AddHttpClient("AuthApi", c => c.BaseAddress = new Uri(apiBase));
builder.Services.AddHttpClient("FinanceApi", c => c.BaseAddress = new Uri(apiBase));
builder.Services.AddScoped<FinanceApiClient>(sp =>
{
    var http = sp.GetRequiredService<IHttpClientFactory>().CreateClient("FinanceApi");
    var tokens = sp.GetRequiredService<AuthTokenStore>();
    var auth = sp.GetRequiredService<AuthService>();
    return new FinanceApiClient(http, tokens, auth);
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

if (!app.Environment.IsProduction())
    app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

// Intentionally no UseAuthentication / UseAuthorization —
// Blazor auth is AuthenticationStateProvider + AuthorizeRouteView only.

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
