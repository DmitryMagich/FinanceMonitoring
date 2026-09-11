using FinanceCalculator.Entities;
using FinanceCalculator.Modules.MonoAPI;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FinanceCalculator.Modules.RestAPI;

public record CreateTransactionRequest(
    Guid? AccountId,
    decimal AmountUah,
    string Category,
    string? Description,
    string? Comment,
    DateTime? Date);

public static class TransactionEndpoints
{
    public static IEndpointRouteBuilder MapTransactionEndpoints(this IEndpointRouteBuilder app)
    {
        // === список ===
        app.MapGet("/api/transactions", async (
            AppDbContext db,
            IOptions<TimeOptions> timeOpts,
            Guid? accountId,
            DateOnly? from,
            DateOnly? to,
            decimal? minAmount,
            decimal? maxAmount,
            string? search,
            int? take) =>
        {
            var offset = TimeSpan.FromHours(timeOpts.Value.TimeZoneOffsetHours);
            var q = db.Transactions.AsQueryable();

            if (accountId is not null)
                q = q.Where(t => t.AccountId == accountId);

            if (from is not null)
            {
                var utcFrom = new DateTimeOffset(from.Value.ToDateTime(TimeOnly.MinValue), offset)
                    .ToUniversalTime().UtcDateTime;
                q = q.Where(t => t.Time >= utcFrom);
            }

            if (to is not null)
            {
                var utcTo = new DateTimeOffset(to.Value.ToDateTime(TimeOnly.MaxValue), offset)
                    .ToUniversalTime().UtcDateTime;
                q = q.Where(t => t.Time <= utcTo);
            }

            if (minAmount is not null)
            {
                var minKop = (long)(minAmount.Value * 100);
                q = q.Where(t => t.Amount >= minKop);
            }

            if (maxAmount is not null)
            {
                var maxKop = (long)(maxAmount.Value * 100);
                q = q.Where(t => t.Amount <= maxKop);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                q = q.Where(t =>
                    (t.Description != null && t.Description.ToLower().Contains(s)) ||
                    (t.CounterName != null && t.CounterName.ToLower().Contains(s)));
            }

            return await q
                .OrderByDescending(t => t.Time)
                .Take(take ?? 200)
                .Select(t => new
                {
                    t.Id,
                    t.AccountId,
                    t.Source,
                    t.Time,
                    t.Description,
                    t.CounterName,
                    t.Mcc,
                    t.Category,
                    AmountUah = t.Amount / 100.0,
                    t.CurrencyCode,
                    t.Comment
                })
                .ToListAsync();
        });

        // === создать ручную ===
        app.MapPost("/api/transactions", async (
            AppDbContext db,
            IOptions<TimeOptions> timeOpts,
            CreateTransactionRequest req) =>
        {
            var offset = TimeSpan.FromHours(timeOpts.Value.TimeZoneOffsetHours);

            if (req.AmountUah == 0)
                return Results.BadRequest("Amount must not be zero");

            if (string.IsNullOrWhiteSpace(req.Category))
                return Results.BadRequest("Category is required");

            Account? account;
            if (req.AccountId is { } aid)
            {
                account = await db.Accounts.FirstOrDefaultAsync(a => a.Id == aid);
                if (account is null)
                    return Results.BadRequest("Account not found");
            }
            else
            {
                account = await db.Accounts.FirstOrDefaultAsync(a => a.MonoAccountId == "");
                if (account is null)
                    return Results.BadRequest("Cash account not found");
            }

            DateTime localDate = req.Date ?? DateTime.UtcNow.Add(offset).Date;
            var utcTime = new DateTimeOffset(
                DateTime.SpecifyKind(localDate, DateTimeKind.Unspecified), offset
            ).ToUniversalTime().UtcDateTime;

            var tx = new Transaction
            {
                Id = Guid.NewGuid(),
                AccountId = account.Id,
                ExternalId = Guid.NewGuid().ToString(),
                Source = TransactionSource.Manual,
                Time = utcTime,
                Description = req.Description,
                Amount = (long)Math.Round(req.AmountUah * 100),
                Balance = 0,
                CurrencyCode = account.CurrencyCode,
                Comment = req.Comment,
                Mcc = 0,
                CounterName = null,
                Category = req.Category.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            db.Transactions.Add(tx);

            if (account.MonoAccountId == "")
            {
                var sum = await db.Transactions
                    .Where(t => t.AccountId == account.Id)
                    .SumAsync(t => t.Amount);
                account.Balance = sum + tx.Amount;
            }

            await db.SaveChangesAsync();

            return Results.Ok(new { tx.Id });
        });

        // === удалить ручную ===
        app.MapDelete("/api/transactions/{id:guid}", async (Guid id, AppDbContext db) =>
        {
            var tx = await db.Transactions.FirstOrDefaultAsync(t => t.Id == id);
            if (tx is null) return Results.NotFound();
            if (tx.Source != TransactionSource.Manual)
                return Results.BadRequest("Only manual transactions can be deleted");

            var account = await db.Accounts.FirstOrDefaultAsync(a => a.Id == tx.AccountId);

            db.Transactions.Remove(tx);
            await db.SaveChangesAsync();

            if (account is not null && account.MonoAccountId == "")
            {
                var sum = await db.Transactions
                    .Where(t => t.AccountId == account.Id)
                    .SumAsync(t => t.Amount);
                account.Balance = sum;
                await db.SaveChangesAsync();
            }

            return Results.Ok(new { deleted = true });
        });

        return app;
    }
}