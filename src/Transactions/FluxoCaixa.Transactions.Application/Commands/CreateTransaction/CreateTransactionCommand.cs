using MediatR;
using FluxoCaixa.Transactions.Domain.Enums;

namespace FluxoCaixa.Transactions.Application.Commands.CreateTransaction;

public sealed record CreateTransactionCommand(
    decimal Amount,
    string Currency,
    ETransactionType TransactionType,
    string? Description
) : IRequest<CreateTransactionResult>;

public record CreateTransactionResult(Guid Id);