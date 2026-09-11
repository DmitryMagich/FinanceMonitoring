using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace FinanceCalculator.Modules.MonoAPI;

public class MonoClient
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _http;
    private readonly MonoRateLimiter _limiter;

    public MonoClient(HttpClient http, MonoRateLimiter limiter)
    {
        _http = http;
        _limiter = limiter;
    }

    /// <summary>Информация о клиенте + список счетов.</summary>
    public async Task<MonoClientInfo?> GetClientInfoAsync(CancellationToken ct = default)
    {
        using var lease = await _limiter.AcquireAsync(ct);

        var resp = await _http.GetAsync("personal/client-info", ct);

        if (resp.StatusCode == HttpStatusCode.TooManyRequests)
            throw new MonoRateLimitException();

        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<MonoClientInfo>(JsonOpts, ct);
    }

    /// <summary>Выписка по счёту за период (unix seconds).</summary>
    /// <remarks>
    /// Максимум 31 день + 1 час (2 682 000 сек).
    /// Лимит: 1 запрос / 60 сек.
    /// Возвращает до 500 транзакций.
    /// </remarks>
    public async Task<List<MonoTransaction>> GetStatementAsync(
        string accountId, long from, long to, CancellationToken ct = default)
    {
        using var lease = await _limiter.AcquireAsync(ct);

        var url = $"personal/statement/{accountId}/{from}/{to}";
        var resp = await _http.GetAsync(url, ct);

        if (resp.StatusCode == HttpStatusCode.TooManyRequests)
            throw new MonoRateLimitException();

        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<List<MonoTransaction>>(JsonOpts, ct)
               ?? new List<MonoTransaction>();
    }

    /// <summary>Курсы валют.</summary>
    public async Task<List<MonoCurrencyInfo>> GetCurrencyAsync(CancellationToken ct = default)
    {
        var resp = await _http.GetAsync("bank/currency", ct);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<List<MonoCurrencyInfo>>(JsonOpts, ct)
               ?? new List<MonoCurrencyInfo>();
    }

    /// <summary>
    /// Полная выписка за период с автоматической пагинацией.
    /// Monobank отдаёт максимум 500 транзакций за запрос и максимум 31 день окном.
    /// Идём с конца (to) в начало (from), сдвигая окно по самой ранней транзакции в чанке.
    /// </summary>
    public async Task<List<MonoTransaction>> GetFullStatementAsync(
        string accountId, DateTime from, DateTime to, CancellationToken ct = default)
    {
        const int maxItems = 500;
        const int maxWindowSeconds = 2_682_000; // 31 день + 1 час

        var all = new List<MonoTransaction>();
        var cursorTo = new DateTimeOffset(DateTime.SpecifyKind(to, DateTimeKind.Utc));
        var fromOffset = new DateTimeOffset(DateTime.SpecifyKind(from, DateTimeKind.Utc));

        while (cursorTo > fromOffset)
        {
            var windowStart = cursorTo.AddSeconds(-maxWindowSeconds);
            var chunkFrom = windowStart < fromOffset ? fromOffset : windowStart;

            var chunk = await GetStatementAsync(
                accountId,
                chunkFrom.ToUnixTimeSeconds(),
                cursorTo.ToUnixTimeSeconds(),
                ct);

            if (chunk.Count == 0) break;

            all.AddRange(chunk);

            if (chunk.Count < maxItems) break;

            var earliest = chunk.Min(t => t.Time);
            cursorTo = DateTimeOffset.FromUnixTimeSeconds(earliest);
        }

        return all;
    }
}