using FinanceCalculator.Entities;
using Microsoft.EntityFrameworkCore;

namespace FinanceCalculator.Modules.RestAPI;

public static class AccountEndpoints
{
    public static IEndpointRouteBuilder MapAccountEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/accounts", async (AppDbContext db) =>
            await db.Accounts
                .Select(a => new
                {
                    a.Id,
                    a.MonoAccountId,
                    a.Type,
                    a.MaskedPan,
                    a.CurrencyCode,
                    BalanceUah = a.Balance / 100.0,
                    a.LastSyncedAt,
                    a.LastTransactionsSyncAt
                })
                .ToListAsync());

        return app;
    }
}