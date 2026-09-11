using FinanceCalculator.Modules.MonoAPI;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FinanceCalculator.Modules.RestAPI;

public static class ChartEndpoints
{
    public static IEndpointRouteBuilder MapChartEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/chart/daily", async (
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

            var q = db.Transactions.Where(t => t.Time >= utcFrom && t.Time <= utcTo);
            if (accountId is not null) q = q.Where(t => t.AccountId == accountId);

            var txs = await q.Select(t => new { t.Time, t.Amount }).ToListAsync();

            var days = new List<object>();
            for (var d = fromLocal; d <= toLocal; d = d.AddDays(1))
            {
                var dayStartUtc = new DateTimeOffset(d.ToDateTime(TimeOnly.MinValue), offset)
                    .ToUniversalTime().UtcDateTime;
                var dayEndUtc = new DateTimeOffset(d.ToDateTime(TimeOnly.MaxValue), offset)
                    .ToUniversalTime().UtcDateTime;

                var dayTxs = txs.Where(t => t.Time >= dayStartUtc && t.Time <= dayEndUtc).ToList();
                var income = dayTxs.Where(t => t.Amount > 0).Sum(t => t.Amount);
                var expense = dayTxs.Where(t => t.Amount < 0).Sum(t => -t.Amount);

                days.Add(new
                {
                    date = d.ToString("yyyy-MM-dd"),
                    income = income / 100.0,
                    expense = expense / 100.0
                });
            }

            return Results.Ok(new { from = fromLocal, to = toLocal, days });
        });

        return app;
    }
}