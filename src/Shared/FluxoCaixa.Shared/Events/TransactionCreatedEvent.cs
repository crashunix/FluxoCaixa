namespace FluxoCaixa.Shared.Events;

public sealed record TransactionCreatedEvent(
    Guid Id,
    decimal Amount,
    string Currency,
    int TransactionType,
    string Description,
    DateTime CreatedAt
);
