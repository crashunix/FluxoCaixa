namespace FluxoCaixa.Transactions.Domain.Interfaces.Messaging;

public interface IMessageBus
{
    Task PublishAsync(string messageType, string messageContent, CancellationToken cancellationToken = default);
    Task PublishBatchAsync(IEnumerable<(string MessageType, string MessageContent)> messages, CancellationToken cancellationToken = default);
}
