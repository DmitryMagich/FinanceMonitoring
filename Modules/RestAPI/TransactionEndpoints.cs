using FinanceCalculator.Entities;
using Microsoft.EntityFrameworkCore;

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
            Guid? accountId,
            DateOnly? from,
            DateOnly? to,
            decimal? minAmount,
            decimal? maxAmount,
            string? search,
            int? take) =>
        {
            var q = db.Transactions.AsQueryable();

            if (accountId is not null)
                q = q.Where(t => t.AccountId == accountId);

            // ФИКС: сравнение с локальным naive-временем, без UTC-конвертации
            if (from is not null)
            {
                var fromDt = from.Value.ToDateTime(TimeOnly.MinValue);
                q = q.Where(t => t.Time >= fromDt);
            }

            if (to is not null)
            {
                var toDt = to.Value.ToDateTime(TimeOnly.MaxValue);
                q = q.Where(t => t.Time <= toDt);
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
                // ФИКС: tie-breaker, иначе две manual-транзакции с одинаковым Time
                // идут в непредсказуемом порядке
                .OrderByDescending(t => t.Time)
                .ThenByDescending(t => t.CreatedAt)
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
            CreateTransactionRequest req) =>
        {
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

            // ФИКС: сохраняем ЛОКАЛЬНОЕ naive-время, никакого UTC.
            // Если дата == сегодня — берём текущее время, чтобы не падало на 00:00.
            DateTime localTime;
            if (req.Date is { } d)
            {
                var datePart = d.Date;
                localTime = datePart == DateTime.Today ? DateTime.Now : datePart;
            }
            else
            {
                localTime = DateTime.Now;
            }

            var tx = new Transaction
            {
                Id = Guid.NewGuid(),
                AccountId = account.Id,
                ExternalId = Guid.NewGuid().ToString(),
                Source = TransactionSource.Manual,
                Time = localTime,
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