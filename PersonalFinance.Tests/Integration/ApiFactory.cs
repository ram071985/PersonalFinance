using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PersonalFinance.Api;
using PersonalFinance.Infrastructure.Data;

namespace PersonalFinance.Tests.Integration;

/// <summary>
/// Real API pipeline against a dedicated SQL Server test database.
/// </summary>
public class ApiFactory : WebApplicationFactory<ApiAssemblyMarker>
{
    public string ConnectionString { get; }

    public ApiFactory()
    {
        ConnectionString =
            Environment.GetEnvironmentVariable("TEST_CONNECTION_STRING")
            ?? "Server=127.0.0.1;Database=PersonalFinance_Test;User Id=sa;Password=TestTest123!;TrustServerCertificate=True;MultipleActiveResultSets=true";
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // UseSetting has high precedence for host configuration
        builder.UseSetting("ConnectionStrings:DefaultConnection", ConnectionString);
        builder.UseSetting("Jwt:Key", "PersonalFinance_Test_Key_MustBeAtLeast32Chars!");
        builder.UseSetting("Jwt:Issuer", "PersonalFinance.Api.Test");
        builder.UseSetting("Jwt:Audience", "PersonalFinance.Web.Test");
        builder.UseSetting("Jwt:ExpireHours", "1");
        builder.UseSetting("Email:Enabled", "false");
        builder.UseSetting("Email:Provider", "None");
        builder.UseSetting("Sms:Enabled", "false");
        builder.UseSetting("KeyVault:Uri", "");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = ConnectionString,
                ["Jwt:Key"] = "PersonalFinance_Test_Key_MustBeAtLeast32Chars!",
                ["Jwt:Issuer"] = "PersonalFinance.Api.Test",
                ["Jwt:Audience"] = "PersonalFinance.Web.Test",
                ["Jwt:ExpireHours"] = "1",
                ["Email:Enabled"] = "false",
                ["Email:Provider"] = "None",
                ["Sms:Enabled"] = "false",
                ["KeyVault:Uri"] = ""
            });
        });
    }

    public async Task ResetDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureDeletedAsync();
        await db.Database.EnsureCreatedAsync();
    }
}
