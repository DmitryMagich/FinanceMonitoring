using System.Threading.RateLimiting;

namespace FinanceCalculator.Modules.MonoAPI;

/// <summary>
/// Глобальный лимитер для Mono API.
/// Monobank: 1 запрос / 60 сек на /personal/* endpoints.
/// </summary>
public class MonoRateLimiter : IDisposable
{
    private readonly RateLimiter _limiter;

    public MonoRateLimiter()
    {
        _limiter = new FixedWindowRateLimiter(new FixedWindowRateLimiterOptions
        {
            PermitLimit = 1,
            Window = TimeSpan.FromSeconds(61), // +1 сек запас, Monobank любит округлять
            QueueLimit = 10,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            AutoReplenishment = true
        });
    }

    /// <summary>Ждёт разрешения. Бросает если очередь переполнена.</summary>
    public async Task<RateLimitLease> AcquireAsync(CancellationToken ct = default)
    {
        var lease = await _limiter.AcquireAsync(1, ct);
        if (!lease.IsAcquired)
            throw new MonoRateLimitException();
        return lease;
    }

    public void Dispose() => _limiter.Dispose();
}

public class MonoRateLimitException : Exception
{
    public MonoRateLimitException() : base("Monobank rate limit hit (1 req / 60 sec)") { }
}