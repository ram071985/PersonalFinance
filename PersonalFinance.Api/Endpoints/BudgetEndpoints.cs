using PersonalFinance.Core.Dtos.Budgets;
using PersonalFinance.Core.Entities;
using PersonalFinance.Core.Interfaces;

namespace PersonalFinance.Api.Endpoints;

public static class BudgetEndpoints
{
    public static IEndpointRouteBuilder MapBudgetEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/budgets").WithTags("Budgets");

        group.MapGet("/", async (IBudgetService service) =>
            Results.Ok(await service.GetAllAsync()));

        group.MapGet("/month/{year:int}/{month:int}", async (int year, int month, IBudgetService service) =>
            Results.Ok(await service.GetByMonthAsync(year, month)));

        group.MapGet("/{id:int}", async (int id, IBudgetService service) =>
        {
            var budget = await service.GetByIdAsync(id);
            return budget is null ? Results.NotFound() : Results.Ok(budget);
        });

        group.MapPost("/", async (CreateBudgetRequest budget, IBudgetService service) =>
        {
            var created = await service.CreateAsync(budget);
            return Results.Created($"/api/budgets/{created.Id}", created);
        });

        group.MapPut("/{id:int}", async (int id, UpdateBudgetRequest input, IBudgetService service) =>
        {
            var updated = await service.UpdateAsync(id, input);
            return updated ? Results.NoContent() : Results.NotFound();
        });

        group.MapDelete("/{id:int}", async (int id, IBudgetService service) =>
        {
            await service.DeleteAsync(id);
            return Results.NoContent();
        });

        return app;
    }
}