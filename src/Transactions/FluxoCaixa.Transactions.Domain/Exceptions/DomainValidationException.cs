namespace FluxoCaixa.Transactions.Domain.Exceptions;

public class DomainValidationException : DomainException
{
    public DomainValidationException(string message, string? paramName = null) 
        : base(message, paramName)
    {
    }
}