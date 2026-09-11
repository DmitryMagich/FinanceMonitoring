namespace FinanceCalculator.Entities;

public class Transaction
{
    public Guid Id { get; set; }

    public Guid AccountId { get; set; }
    public Account? Account { get; set; }

    public string ExternalId { get; set; } = "";

    // откуда пришла операция
    public TransactionSource Source { get; set; } = TransactionSource.Monobank;

    public DateTime Time { get; set; }
    public string? Description { get; set; }

    public long Amount { get; set; }
    public long Balance { get; set; }
    public int CurrencyCode { get; set; }
    public string? Comment { get; set; }
    public int Mcc { get; set; }
    public string? CounterName { get; set; }

    public string Category { get; set; } = "Прочее";

    public DateTime CreatedAt { get; set; }
}