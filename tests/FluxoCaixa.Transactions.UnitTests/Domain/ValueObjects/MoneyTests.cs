using FluxoCaixa.Transactions.Domain.Exceptions;
using FluxoCaixa.Transactions.Domain.ValueObjects;

namespace FluxoCaixa.Transactions.UnitTests.Domain.ValueObjects;

public class MoneyTests
{
    [Fact]
    public void Constructor_WhenValueIsNegative_ShouldThrowDomainValidationException()
    {
        // Arrange
        var value = -10m;
        var currency = "BRL";

        // Act & Assert
        var exception = Assert.Throws<DomainValidationException>(() => new Money(value, currency));
        Assert.Equal("value", exception.ParamName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Constructor_WhenCurrencyIsNullOrEmpty_ShouldThrowDomainValidationException(string currency)
    {
        // Arrange
        var value = 10m;

        // Act & Assert
        var exception = Assert.Throws<DomainValidationException>(() => new Money(value, currency));
        Assert.Equal("currency", exception.ParamName);
    }

    [Fact]
    public void Constructor_WhenValueAndCurrencyAreValid_ShouldCreateMoneyObject()
    {
        // Arrange
        var value = 10m;
        var currency = "USD";

        // Act
        var money = new Money(value, currency);

        // Assert
        Assert.Equal(value, money.Value);
        Assert.Equal(currency, money.Currency);
    }
}