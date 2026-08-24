using FluxoCaixa.Consolidated.Domain.Entities;
using FluxoCaixa.Shared.Events;

namespace FluxoCaixa.Consolidated.Domain.Repositories;

public interface IDailyBalanceRepository
{
    Task<DailyBalance?> GetByDateAsync(DateOnly date, CancellationToken cancellationToken = default);
    Task ProcessBatchAsync(IReadOnlyList<TransactionCreatedEvent> events, CancellationToken cancellationToken = default);
}
