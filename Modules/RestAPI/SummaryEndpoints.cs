using FinanceCalculator.Modules.MonoAPI;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FinanceCalculator.Modules.RestAPI;

public static class SummaryEndpoints
{
    public static IEndpointRouteBuilder MapSummaryEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/summary", async (
            AppDbContext db,
            IOptions<TimeOptions> timeOpts,
            DateOnly? from,
            DateOnly? to,
            Guid? accountId) =>
        {
            var offset = TimeSpan.FromHours(timeOpts.Value.TimeZoneOffsetHours);
            var now = DateTime.UtcNow.Add(offset);

            var fromLocal = from ?? new DateOnly(now.Year, now.Month, 1);
            var toLocal = to ?? new DateOnly(now.Year, now.Month, DateTime.DaysInMonth(now.Year, now.Month));

            var utcFrom = new DateTimeOffset(fromLocal.ToDateTime(TimeOnly.MinValue), offset)
                .ToUniversalTime().UtcDateTime;
            var utcTo = new DateTimeOffset(toLocal.ToDateTime(TimeOnly.MaxValue), offset)
                .ToUniversalTime().UtcDateTime;

            var q = db.Transactions
                .Where(t => t.Time >= utcFrom && t.Time <= utcTo);

            if (accountId is not null)
                q = q.Where(t => t.AccountId == accountId);

            var txs = await q
                .Select(t => new { t.Amount, t.Category })
                .ToListAsync();

            var income = txs.Where(t => t.Amount > 0).Sum(t => t.Amount);
            var expense = txs.Where(t => t.Amount < 0).Sum(t => -t.Amount);

            var topCategories = txs
                .Where(t => t.Amount < 0)
                .GroupBy(t => t.Category)
                .Select(g => new
                {
                    Category = g.Key,
                    Total = g.Sum(t => -t.Amount) / 100.0,
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Total)
                .Take(10)
                .ToList();

            return Results.Ok(new
            {
                from = fromLocal,
                to = toLocal,
                income = income / 100.0,
                expense = expense / 100.0,
                balance = (income - expense) / 100.0,
                transactions = txs.Count,
                topCategories
            });
        });

        return app;
    }
}