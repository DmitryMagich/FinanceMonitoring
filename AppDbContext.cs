using FinanceCalculator.Entities;
using Microsoft.EntityFrameworkCore;

namespace FinanceCalculator;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Transaction> Transactions => Set<Transaction>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<Account>(e =>
        {
            e.HasIndex(a => a.MonoAccountId).IsUnique();
        });

        mb.Entity<Transaction>(e =>
        {
            e.HasIndex(t => new { t.AccountId, t.ExternalId }).IsUnique();
        });
    }
}