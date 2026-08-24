using FluxoCaixa.Consolidated.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FluxoCaixa.Consolidated.Infrastructure.Context;

public class ConsolidatedDbContext : DbContext
{
    public ConsolidatedDbContext(DbContextOptions<ConsolidatedDbContext> options)
        : base(options)
    {
    }

    public DbSet<DailyBalance> DailyBalances => Set<DailyBalance>();
    public DbSet<ProcessedTransaction> ProcessedTransactions => Set<ProcessedTransaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<DailyBalance>(entity =>
        {
            entity.HasKey(e => e.Date);
            entity.Property(e => e.TotalCredit).HasPrecision(18, 2);
            entity.Property(e => e.TotalDebit).HasPrecision(18, 2);
            entity.Property(e => e.Balance).HasPrecision(18, 2);
        });

        modelBuilder.Entity<ProcessedTransaction>(entity =>
        {
            entity.HasKey(e => e.TransactionId);
        });
    }
}
