using PersonalFinance.Api.Filters;
using PersonalFinance.Core.Dtos.Categories;
using PersonalFinance.Core.Enums;
using PersonalFinance.Core.Interfaces;

namespace PersonalFinance.Api.Endpoints;

public static class CategoryEndpoints
{
    public static IEndpointRouteBuilder MapCategoryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/categories")
            .WithTags("Categories")
            .RequireAuthorization();

        group.MapGet("/", async (ICategoryService service) =>
            Results.Ok(await service.GetAllAsync()));

        group.MapGet("/type/{type}", async (CategoryType type, ICategoryService service) =>
            Results.Ok(await service.GetByTypeAsync(type)));

        group.MapGet("/{id:int}", async (int id, ICategoryService service) =>
        {
            var cat = await service.GetByIdAsync(id);
            return cat is null ? Results.NotFound() : Results.Ok(cat);
        });

        group.MapPost("/", async (CreateCategoryRequest category, ICategoryService service) =>
        {
            var created = await service.CreateAsync(category);
            return Results.Created($"/api/categories/{created.Id}", created);
        }).Validate<CreateCategoryRequest>();

        group.MapPut("/{id:int}", async (int id, UpdateCategoryRequest input, ICategoryService service) =>
        {
            var updated = await service.UpdateAsync(id, input);
            return updated ? Results.NoContent() : Results.NotFound();
        }).Validate<UpdateCategoryRequest>();

        group.MapDelete("/{id:int}", async (int id, ICategoryService service) =>
        {
            await service.DeleteAsync(id);
            return Results.NoContent();
        });

        return app;
    }
}