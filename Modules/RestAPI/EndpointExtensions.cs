namespace FinanceCalculator.Modules.RestAPI;

public static class EndpointExtensions
{
    public static IEndpointRouteBuilder MapRestApi(this IEndpointRouteBuilder app)
    {
        app.MapAccountEndpoints();
        app.MapTransactionEndpoints();
        app.MapSummaryEndpoints();
        app.MapChartEndpoints();
        app.MapMonoSyncEndpoints();
        return app;
    }
}