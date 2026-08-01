using PersonalFinance.Api.Filters;
using PersonalFinance.Core.Dtos.Transactions;
using PersonalFinance.Core.Interfaces;

namespace PersonalFinance.Api.Endpoints;

public static class TransactionEndpoints
{
    public static IEndpointRouteBuilder MapTransactionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/transactions")
            .WithTags("Transactions")
            .RequireAuthorization();

        group.MapGet("/", async (ITransactionService service) =>
            Results.Ok(await service.GetAllAsync()));

        group.MapGet("/recent", async (ITransactionService service, int count = 10) =>
            Results.Ok(await service.GetRecentAsync(count)));

        group.MapGet("/account/{accountId:int}", async (int accountId, ITransactionService service) =>
            Results.Ok(await service.GetByAccountIdAsync(accountId)));

        group.MapGet("/{id:int}", async (int id, ITransactionService service) =>
        {
            var tx = await service.GetByIdAsync(id);
            return tx is null ? Results.NotFound() : Results.Ok(tx);
        });

        group.MapPost("/", async (CreateTransactionRequest transaction, ITransactionService service) =>
        {
            var created = await service.CreateAsync(transaction);
            return Results.Created($"/api/transactions/{created.Id}", created);
        }).Validate<CreateTransactionRequest>();

        group.MapPut("/{id:int}", async (int id, UpdateTransactionRequest input, ITransactionService service) =>
        {
            var result = await service.UpdateAsync(id, input);
            return result.IsSuccess
                ? Results.NoContent()
                : Results.NotFound(new { message = result.Error });
        }).Validate<UpdateTransactionRequest>();

        group.MapDelete("/{id:int}", async (int id, ITransactionService service) =>
        {
            var deleted = await service.DeleteAsync(id);
            return deleted ? Results.NoContent() : Results.NotFound();
        });

        return app;
    }
}
