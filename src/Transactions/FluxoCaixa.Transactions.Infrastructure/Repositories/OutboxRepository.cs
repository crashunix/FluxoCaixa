using FluxoCaixa.Transactions.Domain.Entities;
using FluxoCaixa.Transactions.Domain.Interfaces.Repositories;
using FluxoCaixa.Transactions.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace FluxoCaixa.Transactions.Infrastructure.Repositories;

public class OutboxRepository : IOutboxRepository
{
    private readonly TransactionsDbContext _dbContext;

    public OutboxRepository(TransactionsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(OutboxMessage outboxMessage, CancellationToken cancellationToken = default)
    {
        await _dbContext.OutboxMessages.AddAsync(outboxMessage, cancellationToken);
    }

    public async Task<List<OutboxMessage>> GetUnprocessedMessagesAsync(int batchSize, int maxRetries = 5, CancellationToken cancellationToken = default)
    {
        return await _dbContext.OutboxMessages
            .FromSqlInterpolated($"SELECT * FROM \"OutboxMessages\" WHERE \"ProcessedOnUtc\" IS NULL AND \"RetryCount\" < {maxRetries} ORDER BY \"OccurredOnUtc\" ASC LIMIT {batchSize} FOR UPDATE SKIP LOCKED")
            .ToListAsync(cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
