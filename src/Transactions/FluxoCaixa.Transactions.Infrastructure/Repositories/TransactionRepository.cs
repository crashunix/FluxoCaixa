using FluxoCaixa.Transactions.Domain.Entities;
using FluxoCaixa.Transactions.Domain.Interfaces.Repositories;
using FluxoCaixa.Transactions.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace FluxoCaixa.Transactions.Infrastructure.Repositories;

public class TransactionRepository : ITransactionRepository
{
    private readonly TransactionsDbContext _dbContext;

    public TransactionRepository(TransactionsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(Transaction transaction, CancellationToken cancellationToken = default)
    {
        await _dbContext.Transactions.AddAsync(transaction, cancellationToken);
    }

    public async Task<Transaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Transactions.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}