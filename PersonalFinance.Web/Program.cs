using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using PersonalFinance.Components;
using PersonalFinance.Services;
using PersonalFinance.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

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
builder.Services.AddScoped<AuthDelegatingHandler>();

var apiBase = builder.Configuration["ApiBaseUrl"] ?? "https://localhost:7000/";

// Login/register — no bearer
builder.Services.AddHttpClient("AuthApi", c => c.BaseAddress = new Uri(apiBase));

// Finance — with JWT
builder.Services.AddHttpClient<FinanceApiClient>(c => c.BaseAddress = new Uri(apiBase))
    .AddHttpMessageHandler<AuthDelegatingHandler>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode();

app.Run();
