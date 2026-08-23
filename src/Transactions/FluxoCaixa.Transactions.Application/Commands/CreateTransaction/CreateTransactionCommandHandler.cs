using System.Text.Json;
using FluxoCaixa.Shared.Events;
using FluxoCaixa.Transactions.Domain.Entities;
using FluxoCaixa.Transactions.Domain.Interfaces.Messaging;
using FluxoCaixa.Transactions.Domain.Interfaces.Repositories;
using FluxoCaixa.Transactions.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FluxoCaixa.Transactions.Application.Commands.CreateTransaction;

public sealed class CreateTransactionCommandHandler : IRequestHandler<CreateTransactionCommand, CreateTransactionResult>
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly IOutboxRepository _outboxRepository;
    private readonly IOutboxSignal _outboxSignal;
    private readonly ILogger<CreateTransactionCommandHandler> _logger;

    public CreateTransactionCommandHandler(
        ITransactionRepository transactionRepository,
        IOutboxRepository outboxRepository,
        IOutboxSignal outboxSignal,
        ILogger<CreateTransactionCommandHandler> logger)
    {
        _transactionRepository = transactionRepository;
        _outboxRepository = outboxRepository;
        _outboxSignal = outboxSignal;
        _logger = logger;
    }

    public async Task<CreateTransactionResult> Handle(CreateTransactionCommand command, CancellationToken cancellationToken)
    {
        var amount = new Money(command.Amount, command.Currency);
        var description = command.Description ?? string.Empty;
        var transaction = Transaction.Create(amount, command.TransactionType, description);

        await _transactionRepository.AddAsync(transaction, cancellationToken);

        var transactionEvent = new TransactionCreatedEvent(
            transaction.Id,
            transaction.Amount.Value,
            transaction.Amount.Currency,
            (int)transaction.TransactionType,
            transaction.Description,
            transaction.CreatedAt
        );

        var outboxMessage = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = typeof(TransactionCreatedEvent).FullName ?? nameof(TransactionCreatedEvent),
            Content = JsonSerializer.Serialize(transactionEvent),
            OccurredOnUtc = transaction.CreatedAt
        };

        await _outboxRepository.AddAsync(outboxMessage, cancellationToken);

        await _transactionRepository.SaveChangesAsync(cancellationToken);

        _outboxSignal.Notify();

        _logger.LogInformation("Transação {Id} de {Amount} {Currency} ({Type}) criada com sucesso.", 
            transaction.Id, transaction.Amount.Value, transaction.Amount.Currency, transaction.TransactionType);

        return new CreateTransactionResult(transaction.Id);
    }
}