namespace FluxoCaixa.Transactions.Domain.Interfaces.Messaging;

public interface IOutboxSignal
{
    void Notify();
    Task WaitForSignalAsync(TimeSpan timeout, CancellationToken cancellationToken);
}
