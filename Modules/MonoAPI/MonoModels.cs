using System.Text.Json.Serialization;

namespace FinanceCalculator.Modules.MonoAPI;

public class MonoClientInfo
{
    [JsonPropertyName("clientId")]  public string ClientId { get; set; } = "";
    [JsonPropertyName("name")]      public string Name { get; set; } = "";
    [JsonPropertyName("accounts")]  public List<MonoAccount> Accounts { get; set; } = new();
    [JsonPropertyName("jars")]      public List<MonoJar> Jars { get; set; } = new();
}

public class MonoAccount
{
    [JsonPropertyName("id")]         public string Id { get; set; } = "";
    [JsonPropertyName("balance")]    public long Balance { get; set; }   // копейки
    [JsonPropertyName("currencyCode")] public int CurrencyCode { get; set; } 
    [JsonPropertyName("type")]       public string Type { get; set; } = "";
    [JsonPropertyName("maskedPan")]  public List<string> MaskedPan { get; set; } = new();
}

public class MonoJar
{
    [JsonPropertyName("id")]       public string Id { get; set; } = "";
    [JsonPropertyName("title")]    public string Title { get; set; } = "";
    [JsonPropertyName("balance")]  public long Balance { get; set; }
    [JsonPropertyName("currencyCode")] public int CurrencyCode { get; set; }
}

public class MonoTransaction
{
    [JsonPropertyName("id")]           public string Id { get; set; } = "";
    [JsonPropertyName("time")]         public long Time { get; set; }    // unix
    [JsonPropertyName("description")]  public string? Description { get; set; }
    [JsonPropertyName("amount")]       public long Amount { get; set; }  // копейки, со знаком
    [JsonPropertyName("currencyCode")] public int CurrencyCode { get; set; }
    [JsonPropertyName("balance")]      public long Balance { get; set; }
    [JsonPropertyName("comment")]      public string? Comment { get; set; }
    [JsonPropertyName("mcc")]          public int Mcc { get; set; }
    [JsonPropertyName("counterName")]  public string? CounterName { get; set; }
}

public class MonoCurrencyInfo
{
    [JsonPropertyName("currencyCodeA")] public int CurrencyCodeA { get; set; }
    [JsonPropertyName("currencyCodeB")] public int CurrencyCodeB { get; set; }
    [JsonPropertyName("date")]          public long Date { get; set; }
    [JsonPropertyName("rateBuy")]       public double? RateBuy { get; set; }
    [JsonPropertyName("rateSell")]      public double? RateSell { get; set; }
}