using PersonalFinance.Core.Interfaces;

namespace PersonalFinance.Api.Endpoints;

public static class DashboardEndpoints
{
    public static IEndpointRouteBuilder MapDashboardEndpoints(this IEndpointRouteBuilder app)
    {
        var dashboard = app.MapGroup("/api/dashboard")
            .WithTags("Dashboard")
            .RequireAuthorization();

        dashboard.MapGet("/", async (IDashboardService service) =>
            Results.Ok(await service.GetSummaryAsync()));

        return app;
    }
}