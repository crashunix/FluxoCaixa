namespace FluxoCaixa.Transactions.Domain.Interfaces.Messaging;

public interface IMessageBus
{
    Task PublishAsync(string messageType, string messageContent, string? traceId = null, string? spanId = null, CancellationToken cancellationToken = default);
    Task PublishBatchAsync(IEnumerable<(string MessageType, string MessageContent, string? TraceId, string? SpanId)> messages, CancellationToken cancellationToken = default);
}
