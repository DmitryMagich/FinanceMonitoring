namespace FinanceCalculator.Modules.MonoAPI;

public class MonoSyncOptions
{
    public const string SectionName = "MonoSync";

    /// <summary>Как часто запускать авто-синхронизацию (минуты).</summary>
    public int IntervalMinutes { get; set; } = 30;

    /// <summary>За сколько дней тянуть историю, если для счёта ещё ничего не синхронизировано.</summary>
    public int InitialBackfillDays { get; set; } = 30;

    /// <summary>Запускать ли синхронизацию сразу при старте приложения.</summary>
    public bool RunOnStartup { get; set; } = true;
}