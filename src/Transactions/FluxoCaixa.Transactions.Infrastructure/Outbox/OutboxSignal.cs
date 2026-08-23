using System.Threading.Channels;
using FluxoCaixa.Transactions.Domain.Interfaces.Messaging;

namespace FluxoCaixa.Transactions.Infrastructure.Outbox;

public sealed class OutboxSignal : IOutboxSignal
{
    private readonly Channel<bool> _channel = Channel.CreateUnbounded<bool>(new UnboundedChannelOptions
    {
        SingleReader = true,
        SingleWriter = false
    });

    public void Notify()
    {
        _channel.Writer.TryWrite(true);
    }

    public async Task WaitForSignalAsync(TimeSpan timeout, CancellationToken cancellationToken)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(timeout);

        try
        {
            if (await _channel.Reader.WaitToReadAsync(cts.Token))
            {
                // Drena todos os sinais acumulados
                while (_channel.Reader.TryRead(out _)) { }
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // Timeout expirado sem novos sinais
        }
    }
}
