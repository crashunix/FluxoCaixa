using FluxoCaixa.Transactions.Domain.Entities;
using FluxoCaixa.Transactions.Domain.Enums;
using FluxoCaixa.Transactions.Domain.Exceptions;
using FluxoCaixa.Transactions.Domain.ValueObjects;

namespace FluxoCaixa.Transactions.UnitTests.Domain.Entities;

public class TransactionTests
{
    [Fact]
    public void Create_WhenValidParameters_ShouldCreateTransaction()
    {
        // Arrange
        var amount = new Money(100, "USD");
        var transactionType = ETransactionType.Credit;

        // Act
        var transaction = Transaction.Create(amount, transactionType, "Test transaction");

        // Assert
        Assert.NotEqual(Guid.Empty, transaction.Id);
        Assert.Equal(amount, transaction.Amount);
        Assert.NotEqual(DateTime.MinValue, transaction.CreatedAt);
        Assert.Equal(transactionType, transaction.TransactionType);
    }

    [Fact]
    public void Create_WhenAmountIsNull_ShouldThrowDomainValidationException()
    {
        // Arrange
        Money amount = null!;
        var transactionType = ETransactionType.Credit;

        // Act & Assert
        var exception = Assert.Throws<DomainValidationException>(() => Transaction.Create(amount, transactionType, "Test transaction"));
        Assert.Equal("amount", exception.ParamName);
    }

    [Fact]
    public void Restore_WhenValidParameters_ShouldRestoreTransaction()
    {
        // Arrange
        var id = Guid.NewGuid();
        var amount = new Money(100, "USD");
        var createdAt = DateTime.UtcNow;
        var transactionType = ETransactionType.Credit;
        var description = "Restored transaction";

        // Act
        var transaction = Transaction.Restore(id, amount, createdAt, transactionType, description);

        // Assert
        Assert.Equal(id, transaction.Id);
        Assert.Equal(amount, transaction.Amount);
        Assert.Equal(createdAt, transaction.CreatedAt);
        Assert.Equal(transactionType, transaction.TransactionType);
        Assert.Equal(description, transaction.Description);
    }

    [Fact]
    public void Restore_WhenIdIsEmpty_ShouldThrowDomainValidationException()
    {
        // Arrange
        var id = Guid.Empty;
        var amount = new Money(100, "USD");
        var createdAt = DateTime.UtcNow;
        var transactionType = ETransactionType.Credit;
        var description = "Restored transaction";

        // Act & Assert
        var exception = Assert.Throws<DomainValidationException>(() => Transaction.Restore(id, amount, createdAt, transactionType, description));
        Assert.Equal("id", exception.ParamName);
    }   

    [Fact]
    public void Restore_WhenCreatedAtIsDefault_ShouldThrowDomainValidationException()
    {
        // Arrange
        var id = Guid.NewGuid();
        var amount = new Money(100, "USD");
        var createdAt = DateTime.MinValue;
        var transactionType = ETransactionType.Credit;
        var description = "Restored transaction";

        // Act & Assert
        var exception = Assert.Throws<DomainValidationException>(() => Transaction.Restore(id, amount, createdAt, transactionType, description));
        Assert.Equal("createdAt", exception.ParamName);
    }

    [Fact]
    public void Restore_WhenTransactionTypeIsInvalid_ShouldThrowDomainValidationException()
    {
        // Arrange
        var id = Guid.NewGuid();
        var amount = new Money(100, "USD");
        var createdAt = DateTime.UtcNow;
        var transactionType = (ETransactionType)999; // Invalid transaction type
        var description = "Restored transaction";

        // Act & Assert
        var exception = Assert.Throws<DomainValidationException>(() => Transaction.Restore(id, amount, createdAt, transactionType, description));
        Assert.Equal("transactionType", exception.ParamName);
    }

    
}