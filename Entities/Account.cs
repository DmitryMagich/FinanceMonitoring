namespace FinanceCalculator.Entities;

public class Account
{
    public Guid Id { get; set; }

    public string MonoAccountId { get; set; } = "";

    public int CurrencyCode { get; set; }
    public string Type { get; set; } = "";
    public string? MaskedPan { get; set; }

    public long Balance { get; set; }
    public DateTime LastSyncedAt { get; set; }
    public DateTime? LastTransactionsSyncAt { get; set; }

    public List<Transaction> Transactions { get; set; } = new();
}