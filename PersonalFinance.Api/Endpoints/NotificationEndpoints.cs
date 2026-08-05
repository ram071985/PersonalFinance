using PersonalFinance.Core.Interfaces;

namespace PersonalFinance.Api.Endpoints;

public static class NotificationEndpoints
{
    public static IEndpointRouteBuilder MapNotificationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/notifications")
            .WithTags("Notifications")
            .RequireAuthorization();

        group.MapGet("/", async (INotificationService service, int take = 20) =>
            Results.Ok(await service.GetRecentAsync(Math.Clamp(take, 1, 50))));

        group.MapGet("/unread-count", async (INotificationService service) =>
            Results.Ok(new { count = await service.GetUnreadCountAsync() }));

        group.MapPost("/{id:int}/read", async (int id, INotificationService service) =>
        {
            await service.MarkReadAsync(id);
            return Results.NoContent();
        });

        group.MapPost("/read-all", async (INotificationService service) =>
        {
            await service.MarkAllReadAsync();
            return Results.NoContent();
        });

        return app;
    }
}