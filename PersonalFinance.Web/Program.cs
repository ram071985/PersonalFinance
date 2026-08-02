using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using PersonalFinance.Components;
using PersonalFinance.Services;
using PersonalFinance.Web.Services;

var builder = WebApplication.CreateBuilder(args);

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
builder.Services.AddScoped<ToastService>();
var apiBase = builder.Configuration["ApiBaseUrl"] ?? "https://localhost:7000/";

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
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
