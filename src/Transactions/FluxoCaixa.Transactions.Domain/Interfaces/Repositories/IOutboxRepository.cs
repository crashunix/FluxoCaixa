using FluxoCaixa.Transactions.Domain.Entities;

namespace FluxoCaixa.Transactions.Domain.Interfaces.Repositories;

public interface IOutboxRepository
{
    Task AddAsync(OutboxMessage outboxMessage, CancellationToken cancellationToken = default);
    Task<List<OutboxMessage>> GetUnprocessedMessagesAsync(int batchSize, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
