using Microsoft.AspNetCore.DataProtection;

namespace PersonalFinance.Infrastructure.Plaid;

/// <summary>Encrypts Plaid access tokens at rest (ASP.NET Data Protection).</summary>
public class PlaidTokenProtector
{
    private readonly IDataProtector _protector;

    public PlaidTokenProtector(IDataProtectionProvider provider) =>
        _protector = provider.CreateProtector("PersonalFinance.Plaid.AccessToken.v1");

    public string Protect(string accessToken) => _protector.Protect(accessToken);

    public string Unprotect(string protectedToken) => _protector.Unprotect(protectedToken);
}