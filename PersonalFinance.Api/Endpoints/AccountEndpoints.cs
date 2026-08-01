using PersonalFinance.Api.Filters;
using PersonalFinance.Core.Dtos.Accounts;
using PersonalFinance.Core.Interfaces;

namespace PersonalFinance.Api.Endpoints;

public static class AccountEndpoints
{
    public static IEndpointRouteBuilder MapAccountEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/accounts")
            .WithTags("Accounts")
            .RequireAuthorization();

        group.MapGet("/", async (IAccountService service) =>
            Results.Ok(await service.GetAllAsync()));

        group.MapGet("/{id:int}", async (int id, IAccountService service) =>
        {
            var account = await service.GetByIdAsync(id);
            return account is null ? Results.NotFound() : Results.Ok(account);
        });

        group.MapGet("/total-balance", async (IAccountService service) =>
            Results.Ok(new { TotalBalance = await service.GetTotalBalanceAsync() }));

        group.MapPost("/", async (CreateAccountRequest account, IAccountService service) =>
        {
            var created = await service.CreateAsync(account);
            return Results.Created($"/api/accounts/{created.Id}", created);
        }).Validate<CreateAccountRequest>();

        group.MapPut("/{id:int}", async (int id, UpdateAccountRequest input, IAccountService service) =>
        {
            var result = await service.UpdateAsync(id, input);
            return result.IsSuccess
                ? Results.NoContent()
                : Results.NotFound(new { message = result.Error });
        }).Validate<UpdateAccountRequest>();

        group.MapDelete("/{id:int}", async (int id, IAccountService service) =>
        {
            var deleted = await service.DeleteAsync(id);
            return deleted ? Results.NoContent() : Results.NotFound();
        });

        return app;
    }
}