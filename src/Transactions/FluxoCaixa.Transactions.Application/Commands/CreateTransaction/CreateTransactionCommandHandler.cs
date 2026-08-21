using FluxoCaixa.Transactions.Domain.Entities;
using FluxoCaixa.Transactions.Domain.Interfaces.Repositories;
using FluxoCaixa.Transactions.Domain.ValueObjects;
using MediatR;

namespace FluxoCaixa.Transactions.Application.Commands.CreateTransaction;

public sealed class CreateTransactionCommandHandler : IRequestHandler<CreateTransactionCommand, CreateTransactionResult>
{
    private readonly ITransactionRepository _transactionRepository;

    public CreateTransactionCommandHandler(ITransactionRepository transactionRepository)
    {
        _transactionRepository = transactionRepository;
    }

    public async Task<CreateTransactionResult> Handle(CreateTransactionCommand command, CancellationToken cancellationToken)
    {
        var amount = new Money(command.Amount, command.Currency);
        var description = command.Description ?? string.Empty;
        var transaction = Transaction.Create(amount, command.TransactionType, description);

        await _transactionRepository.AddAsync(transaction, cancellationToken);
        await _transactionRepository.SaveChangesAsync(cancellationToken);

        return new CreateTransactionResult(transaction.Id);
    }
}