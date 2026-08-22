using FluxoCaixa.Transactions.Domain.Exceptions;

namespace FluxoCaixa.Transactions.Domain.ValueObjects;

public sealed record Money {
    public decimal Value { get; }
    public string Currency { get; }

    public Money(decimal value, string currency = "BRL") {
        if (value <= 0) {
            throw new DomainValidationException("Value must be a positive number.", nameof(value));
        }
        if (string.IsNullOrWhiteSpace(currency)) {
            throw new DomainValidationException("Currency cannot be null or whitespace.", nameof(currency));
        }
        if (currency.Length != 3) {
            throw new DomainValidationException("Currency must be a 3-letter ISO code.", nameof(currency));
        }
        Value = value;
        Currency = currency;
    }
}
