using FluxoCaixa.Transactions.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FluxoCaixa.Transactions.Infrastructure.Context;

public class TransactionsDbContext : DbContext
{
    public TransactionsDbContext(DbContextOptions<TransactionsDbContext> options) : base(options)
    {
    }

    public DbSet<Transaction> Transactions => Set<Transaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TransactionsDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}