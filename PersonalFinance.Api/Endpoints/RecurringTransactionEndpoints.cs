using PersonalFinance.Api.Filters;
using PersonalFinance.Core.Dtos.Recurring;
using PersonalFinance.Core.Interfaces;

namespace PersonalFinance.Api.Endpoints;

public static class RecurringTransactionEndpoints
{
    public static IEndpointRouteBuilder MapRecurringTransactionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/recurring-transactions")
            .WithTags("RecurringTransactions")
            .RequireAuthorization();

        group.MapGet("/", async (IRecurringTransactionService service) =>
            Results.Ok(await service.GetAllAsync()));

        group.MapPost("/", async (CreateRecurringTransactionRequest request, IRecurringTransactionService service) =>
        {
            var created = await service.CreateAsync(request);
            return Results.Created($"/api/recurring-transactions/{created.Id}", created);
        }).Validate<CreateRecurringTransactionRequest>();

        group.MapPost("/{id:int}/generate", async (int id, IRecurringTransactionService service) =>
        {
            var tx = await service.GenerateDueAsync(id);
            return tx is null ? Results.BadRequest(new { message = "Not due or already generated this month." }) : Results.Ok(tx);
        });

        group.MapDelete("/{id:int}", async (int id, IRecurringTransactionService service) =>
        {
            var deleted = await service.DeleteAsync(id);
            return deleted ? Results.NoContent() : Results.NotFound();
        });

        // Manual kick for ops/dev (still requires auth; generates only current user via GenerateDue on each owned template)
        group.MapPost("/generate-mine", async (IRecurringTransactionService service) =>
        {
            var all = await service.GetAllAsync();
            var generated = 0;
            foreach (var r in all)
            {
                var tx = await service.GenerateDueAsync(r.Id);
                if (tx is not null) generated++;
            }
            return Results.Ok(new { generated });
        });

        return app;
    }
}
