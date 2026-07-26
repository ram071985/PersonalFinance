using PersonalFinance.Core.Interfaces;

namespace PersonalFinance.Api.Endpoints;

public static class DashboardEndpoints
{
    public static IEndpointRouteBuilder MapDashboardEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/dashboard").WithTags("Dashboard");

        group.MapGet("/", async (IDashboardService service) =>
            Results.Ok(await service.GetSummaryAsync()));

        return app;
    }
}