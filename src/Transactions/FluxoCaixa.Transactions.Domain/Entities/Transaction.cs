using FluxoCaixa.Transactions.Domain.ValueObjects;
using FluxoCaixa.Transactions.Domain.Enums;
using FluxoCaixa.Transactions.Domain.Exceptions;

namespace FluxoCaixa.Transactions.Domain.Entities;

public class Transaction
{
    public Guid Id { get; private set; }
    public Money Amount { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }
    public ETransactionType TransactionType { get; private set; }
    public string Description { get; private set; } = string.Empty;

    private Transaction() { }
    private Transaction(Guid id, Money amount, DateTime createdAt, ETransactionType transactionType, string description)
    {
        if(id == Guid.Empty)
        {
            throw new DomainValidationException("Id cannot be empty", nameof(id));
        }
        if(amount == null)
        {
            throw new DomainValidationException("Amount cannot be null", nameof(amount));
        }
        if(createdAt == DateTime.MinValue)
        {
            throw new DomainValidationException("CreatedAt cannot be the default value", nameof(createdAt));
        }
        if(!Enum.IsDefined(typeof(ETransactionType), transactionType))
        {
            throw new DomainValidationException("Invalid transaction type", nameof(transactionType));
        }
        Id = id;
        Amount = amount;
        CreatedAt = createdAt;
        TransactionType = transactionType;
        Description = description;
    }

    public static Transaction Create(Money amount, ETransactionType transactionType, string description)
    {
        return new Transaction(Guid.NewGuid(), amount, DateTime.UtcNow, transactionType, description);
    }

}