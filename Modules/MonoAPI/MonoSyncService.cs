using FinanceCalculator.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FinanceCalculator.Modules.MonoAPI;

public class MonoSyncService
{
    private readonly MonoClient _mono;
    private readonly AppDbContext _db;
    private readonly ILogger<MonoSyncService> _log;

    public MonoSyncService(MonoClient mono, AppDbContext db, ILogger<MonoSyncService> log)
    {
        _mono = mono;
        _db = db;
        _log = log;
    }

    public async Task<(int created, int updated)> SyncAccountsAsync(CancellationToken ct = default)
    {
        var info = await _mono.GetClientInfoAsync(ct);
        if (info is null) return (0, 0);

        var now = DateTime.UtcNow;
        int created = 0, updated = 0;

        foreach (var acc in info.Accounts)
        {
            var existing = await _db.Accounts
                .FirstOrDefaultAsync(a => a.MonoAccountId == acc.Id, ct);

            var maskedPan = acc.MaskedPan.FirstOrDefault();

            if (existing is null)
            {
                _db.Accounts.Add(new Account
                {
                    Id = Guid.NewGuid(),
                    MonoAccountId = acc.Id,
                    CurrencyCode = acc.CurrencyCode,
                    Type = acc.Type,
                    MaskedPan = maskedPan,
                    Balance = acc.Balance,
                    LastSyncedAt = now
                });
                created++;
            }
            else
            {
                existing.Balance = acc.Balance;
                existing.CurrencyCode = acc.CurrencyCode;
                existing.Type = acc.Type;
                existing.MaskedPan = maskedPan;
                existing.LastSyncedAt = now;
                updated++;
            }
        }

        await _db.SaveChangesAsync(ct);
        return (created, updated);
    }

    public async Task<(int added, int skipped)> SyncTransactionsAsync(
        Guid accountId, DateTime from, DateTime to, CancellationToken ct = default)
    {
        var account = await _db.Accounts
            .FirstOrDefaultAsync(a => a.Id == accountId, ct)
            ?? throw new InvalidOperationException($"Account {accountId} not found");

        if (string.IsNullOrEmpty(account.MonoAccountId))
            throw new InvalidOperationException($"Account {accountId} has no MonoAccountId");

        var statement = await _mono.GetFullStatementAsync(account.MonoAccountId, from, to, ct);

        var existingIds = await _db.Transactions
            .Where(t => t.AccountId == accountId)
            .Select(t => t.ExternalId)
            .ToHashSetAsync(ct);

        var now = DateTime.UtcNow;
        int added = 0, skipped = 0;

        foreach (var tx in statement)
        {
            if (existingIds.Contains(tx.Id))
            {
                skipped++;
                continue;
            }

            _db.Transactions.Add(new Transaction
            {
                Id = Guid.NewGuid(),
                AccountId = accountId,
                ExternalId = tx.Id,
                Source = TransactionSource.Monobank,   
                Time = DateTimeOffset.FromUnixTimeSeconds(tx.Time).UtcDateTime,
                Description = tx.Description,
                Amount = tx.Amount,
                Balance = tx.Balance,
                CurrencyCode = tx.CurrencyCode,
                Comment = tx.Comment,
                Mcc = tx.Mcc,
                CounterName = tx.CounterName,
                Category = MccMapper.GetCategory(tx.Mcc),
                CreatedAt = now
            });
            added++;
        }

        if (added > 0)
            await _db.SaveChangesAsync(ct);

        return (added, skipped);
    }

    public async Task<SyncAllResult> SyncAllTransactionsAsync(
        int initialBackfillDays, CancellationToken ct = default)
    {
        var accounts = await _db.Accounts
            .Where(a => a.MonoAccountId != "")
            .ToListAsync(ct);

        var now = DateTime.UtcNow;
        int totalAdded = 0, totalSkipped = 0, processed = 0;

        foreach (var account in accounts)
        {
            if (ct.IsCancellationRequested) break;

            var from = account.LastTransactionsSyncAt
                       ?? now.AddDays(-initialBackfillDays);

            from = from.AddDays(-1);

            if (from >= now)
            {
                processed++;
                continue;
            }

            try
            {
                var (added, skipped) = await SyncTransactionsAsync(account.Id, from, now, ct);

                account.LastTransactionsSyncAt = now;
                await _db.SaveChangesAsync(ct);

                totalAdded += added;
                totalSkipped += skipped;
                processed++;

                _log.LogInformation(
                    "Synced account {AccountId} ({Masked}): from={From:u} added={Added} skipped={Skipped}",
                    account.Id, account.MaskedPan, from, added, skipped);
            }
            catch (MonoRateLimitException ex)
            {
                _log.LogWarning(ex, "Rate limit for account {AccountId}, will retry next cycle", account.Id);
                break;
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Failed to sync account {AccountId}, continuing with next", account.Id);
            }
        }

        return new SyncAllResult(processed, totalAdded, totalSkipped);
    }
}

public record SyncAllResult(int Accounts, int Added, int Skipped);