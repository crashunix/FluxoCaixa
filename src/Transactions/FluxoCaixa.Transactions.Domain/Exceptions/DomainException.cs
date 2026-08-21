namespace FluxoCaixa.Transactions.Domain.Exceptions;

public abstract class DomainException : Exception
{
    public string? ParamName { get; }

    public DomainException(string message, string? paramName = null) 
        : base(message)
    {
        ParamName = paramName;
    }
}