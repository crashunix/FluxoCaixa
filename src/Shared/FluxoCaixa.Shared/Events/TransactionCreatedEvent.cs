namespace FluxoCaixa.Shared.Events;

public record TransactionCreatedEvent(
    Guid Id,
    decimal Amount,
    string Type,
    DateTime CreatedAt,
    string? Description
);
