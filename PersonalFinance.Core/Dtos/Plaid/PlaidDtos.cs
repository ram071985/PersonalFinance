namespace PersonalFinance.Core.Dtos.Plaid;

public class PlaidLinkTokenResponse
{
    public string LinkToken { get; set; } = "";
    public string Expiration { get; set; } = "";
}

public class PlaidExchangeRequest
{
    public string PublicToken { get; set; } = "";
    public string? InstitutionId { get; set; }
    public string? InstitutionName { get; set; }
}

public class PlaidItemDto
{
    public int Id { get; set; }
    public string ItemId { get; set; } = "";
    public string? InstitutionName { get; set; }
    public string Status { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public DateTime? LastSyncedAt { get; set; }
    public string? LastError { get; set; }
}

public class PlaidSyncResultDto
{
    public int AccountsUpserted { get; set; }
    public int TransactionsAdded { get; set; }
    public int TransactionsModified { get; set; }
    public int TransactionsRemoved { get; set; }
    public DateTime? LastSyncedAt { get; set; }
}