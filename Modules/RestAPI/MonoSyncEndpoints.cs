using FinanceCalculator.Modules.MonoAPI;

namespace FinanceCalculator.Modules.RestAPI;

public static class MonoSyncEndpoints
{
    public static IEndpointRouteBuilder MapMonoSyncEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/mono/sync/accounts", async (MonoSyncService sync, CancellationToken ct) =>
        {
            var (created, updated) = await sync.SyncAccountsAsync(ct);
            return Results.Ok(new { created, updated });
        });

        app.MapPost("/mono/sync/transactions/{accountId:guid}",
            async (Guid accountId, DateTime? from, DateTime? to,
                MonoSyncService sync, CancellationToken ct) =>
            {
                var toDate = to ?? DateTime.UtcNow;
                var fromDate = from ?? toDate.AddDays(-31);

                if (toDate <= fromDate)
                    return Results.BadRequest("to must be greater than from");

                var (added, skipped) = await sync.SyncTransactionsAsync(accountId, fromDate, toDate, ct);
                return Results.Ok(new { added, skipped, from = fromDate, to = toDate });
            });

        return app;
    }
}