using FluxoCaixa.Transactions.Domain.Entities;
using FluxoCaixa.Transactions.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;

namespace FluxoCaixa.Transactions.Infrastructure.Context;

public class TransactionsDbContext : DbContext
{
    public TransactionsDbContext(DbContextOptions<TransactionsDbContext> options) : base(options)
    {
    }

    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TransactionsDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}