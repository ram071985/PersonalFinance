using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using PersonalFinance.Core.Dtos.Plaid;
using PersonalFinance.Core.Interfaces;
using PersonalFinance.Infrastructure.Plaid;

namespace PersonalFinance.Api.Endpoints;

public static class PlaidEndpoints
{
    public static IEndpointRouteBuilder MapPlaidEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/plaid")
            .WithTags("Plaid");

        // Authenticated app APIs
        var authed = group.MapGroup("").RequireAuthorization();

        authed.MapPost("/link-token", async (IPlaidService plaid, CancellationToken ct) =>
        {
            var token = await plaid.CreateLinkTokenAsync(ct);
            return Results.Ok(token);
        }).RequireRateLimiting("plaid");

        authed.MapPost("/exchange", async (PlaidExchangeRequest request, IPlaidService plaid, CancellationToken ct) =>
        {
            var item = await plaid.ExchangePublicTokenAsync(request, ct);
            return Results.Ok(item);
        }).RequireRateLimiting("plaid");

        authed.MapGet("/items", async (IPlaidService plaid, CancellationToken ct) =>
            Results.Ok(await plaid.GetItemsAsync(ct)));

        authed.MapPost("/items/{id:int}/sync", async (int id, IPlaidService plaid, CancellationToken ct) =>
            Results.Ok(await plaid.SyncItemAsync(id, ct)));

        authed.MapPost("/sync-all", async (IPlaidService plaid, CancellationToken ct) =>
            Results.Ok(await plaid.SyncAllForCurrentUserAsync(ct)));

        authed.MapDelete("/items/{id:int}", async (int id, IPlaidService plaid, CancellationToken ct) =>
        {
            var ok = await plaid.RemoveItemAsync(id, ct);
            return ok ? Results.NoContent() : Results.NotFound();
        });

        // Webhook — no user JWT. Optional shared secret. Does not return transaction data.
        group.MapPost("/webhook", async (
            HttpRequest http,
            IPlaidService plaid,
            IOptions<PlaidOptions> options,
            ILoggerFactory loggerFactory,
            CancellationToken ct) =>
        {
            var logger = loggerFactory.CreateLogger("PlaidWebhook");
            var opts = options.Value;

            if (!string.IsNullOrWhiteSpace(opts.WebhookSecret))
            {
                if (!http.Query.TryGetValue("key", out var key) || key != opts.WebhookSecret)
                    return Results.Unauthorized();
            }

            using var doc = await JsonDocument.ParseAsync(http.Body, cancellationToken: ct);
            var root = doc.RootElement;
            var webhookType = root.TryGetProperty("webhook_type", out var wt) ? wt.GetString() : null;
            var webhookCode = root.TryGetProperty("webhook_code", out var wc) ? wc.GetString() : null;

            string? itemId = null;
            if (root.TryGetProperty("item_id", out var iid))
                itemId = iid.GetString();

            logger.LogInformation("Plaid webhook {Type}/{Code} ItemId={ItemId}", webhookType, webhookCode, itemId);

            if (string.IsNullOrEmpty(itemId))
                return Results.Ok();

            // Transactions updates or initial product ready
            if (string.Equals(webhookType, "TRANSACTIONS", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(webhookType, "ITEM", StringComparison.OrdinalIgnoreCase))
            {
                await plaid.SyncByPlaidItemIdAsync(itemId, ct);
            }

            return Results.Ok();
        });

        return app;
    }
}
