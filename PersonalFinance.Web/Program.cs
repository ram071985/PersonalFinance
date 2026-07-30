using PersonalFinance.Components;
using PersonalFinance.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient<FinanceApiClient>((serviceProvider, client) =>
{
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    
    // This will correctly pick up Azure App Settings
    var apiBaseUrl = configuration["ApiBaseUrl"];
    
    if (string.IsNullOrWhiteSpace(apiBaseUrl))
    {
        apiBaseUrl = "https://localhost:7000/";
    }
    
    client.BaseAddress = new Uri(apiBaseUrl.TrimEnd('/') + "/");
});

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

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
