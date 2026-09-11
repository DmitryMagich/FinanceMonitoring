using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FinanceCalculator.Modules.MonoAPI;

public class MonoAutoSyncService : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly MonoSyncOptions _options;
    private readonly ILogger<MonoAutoSyncService> _log;

    public MonoAutoSyncService(
        IServiceScopeFactory scopes,
        IOptions<MonoSyncOptions> options,
        ILogger<MonoAutoSyncService> log)
    {
        _scopes = scopes;
        _options = options.Value;
        _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        _log.LogInformation("Mono auto-sync started. Interval = {Interval} min",
            _options.IntervalMinutes);

        if (_options.RunOnStartup)
        {
            // небольшая задержка, чтобы приложение успело подняться
            try { await Task.Delay(TimeSpan.FromSeconds(10), ct); }
            catch (OperationCanceledException) { return; }

            await RunCycleAsync(ct);
        }

        var interval = TimeSpan.FromMinutes(_options.IntervalMinutes);

        while (!ct.IsCancellationRequested)
        {
            try { await Task.Delay(interval, ct); }
            catch (OperationCanceledException) { break; }

            await RunCycleAsync(ct);
        }

        _log.LogInformation("Mono auto-sync stopped");
    }

    private async Task RunCycleAsync(CancellationToken ct)
    {
        try
        {
            using var scope = _scopes.CreateScope();
            var sync = scope.ServiceProvider.GetRequiredService<MonoSyncService>();

            _log.LogInformation("Auto-sync cycle: start");

            var (created, updated) = await sync.SyncAccountsAsync(ct);
            _log.LogInformation("Auto-sync cycle: accounts +{Created} ~{Updated}", created, updated);

            var result = await sync.SyncAllTransactionsAsync(_options.InitialBackfillDays, ct);
            _log.LogInformation(
                "Auto-sync cycle: done. accounts={Accounts} added={Added} skipped={Skipped}",
                result.Accounts, result.Added, result.Skipped);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Auto-sync cycle failed, will retry next interval");
        }
    }
}