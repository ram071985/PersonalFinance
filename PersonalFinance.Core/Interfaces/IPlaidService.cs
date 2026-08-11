using PersonalFinance.Core.Dtos.Plaid;

namespace PersonalFinance.Core.Interfaces;

public interface IPlaidService
{
    Task<PlaidLinkTokenResponse> CreateLinkTokenAsync(CancellationToken ct = default);
    Task<PlaidItemDto> ExchangePublicTokenAsync(PlaidExchangeRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<PlaidItemDto>> GetItemsAsync(CancellationToken ct = default);
    Task<PlaidSyncResultDto> SyncItemAsync(int plaidItemId, CancellationToken ct = default);
    Task<PlaidSyncResultDto> SyncAllForCurrentUserAsync(CancellationToken ct = default);
    Task<bool> RemoveItemAsync(int plaidItemId, CancellationToken ct = default);
    /// <summary>Webhook-driven sync by Plaid item_id (no HTTP user context).</summary>
    Task SyncByPlaidItemIdAsync(string plaidItemId, CancellationToken ct = default);
}