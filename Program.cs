using FinanceCalculator;
using FinanceCalculator.Entities;
using FinanceCalculator.Modules;
using FinanceCalculator.Modules.MonoAPI;
using FinanceCalculator.Modules.RestAPI;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<TimeOptions>(
    builder.Configuration.GetSection(TimeOptions.SectionName));

builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlite(builder.Configuration.GetConnectionString("Default")));

builder.Services.Configure<MonoSyncOptions>(
    builder.Configuration.GetSection(MonoSyncOptions.SectionName));

builder.Services.AddSingleton<MonoRateLimiter>();

builder.Services.AddHttpClient<MonoClient>(client =>
{
    client.BaseAddress = new Uri("https://api.monobank.ua/");
    client.DefaultRequestHeaders.Add("X-Token",
        builder.Configuration["Monobank:Token"]);
});

builder.Services.AddScoped<MonoSyncService>();
builder.Services.AddHostedService<MonoAutoSyncService>();

var app = builder.Build();

// миграции + WAL + авто-создание счёта «Наличные»
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
    db.Database.ExecuteSqlRaw("PRAGMA journal_mode=WAL;");

    var cash = db.Accounts.FirstOrDefault(a => a.MonoAccountId == "");
    if (cash is null)
    {
        db.Accounts.Add(new Account
        {
            Id = Guid.NewGuid(),
            MonoAccountId = "",
            Type = "cash",
            MaskedPan = "Наличные",
            CurrencyCode = 980,
            Balance = 0,
            LastSyncedAt = DateTime.UtcNow
        });
        db.SaveChanges();
    }
}

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapRestApi();

app.Run();