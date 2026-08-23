using FluxoCaixa.Transactions.Domain.Interfaces.Messaging;
using FluxoCaixa.Transactions.Domain.Interfaces.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FluxoCaixa.Transactions.Infrastructure.Outbox;

public sealed class OutboxProcessorBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOutboxSignal _outboxSignal;
    private readonly ILogger<OutboxProcessorBackgroundService> _logger;
    private static readonly TimeSpan FallbackTimeout = TimeSpan.FromSeconds(30);

    public OutboxProcessorBackgroundService(
        IServiceScopeFactory scopeFactory,
        IOutboxSignal outboxSignal,
        ILogger<OutboxProcessorBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _outboxSignal = outboxSignal;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Outbox Processor Background Service iniciado.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOutboxMessagesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar mensagens da fila Outbox.");
            }

            await _outboxSignal.WaitForSignalAsync(FallbackTimeout, stoppingToken);
        }
    }

    private async Task ProcessOutboxMessagesAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var outboxRepository = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        var messages = await outboxRepository.GetUnprocessedMessagesAsync(100, cancellationToken);

        if (messages.Count == 0)
        {
            return;
        }

        try
        {
            var batchPayloads = messages.Select(m => (m.Type, m.Content));

            await messageBus.PublishBatchAsync(batchPayloads, cancellationToken);

            var now = DateTime.UtcNow;
            foreach (var message in messages)
            {
                message.ProcessedOnUtc = now;
                message.Error = null;
            }

            await outboxRepository.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Lote de {Count} mensagem(ns) do Outbox publicado no RabbitMQ e atualizado no banco.", messages.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao publicar lote de {Count} mensagem(ns) do Outbox no RabbitMQ.", messages.Count);
            foreach (var message in messages)
            {
                message.Error = ex.Message;
            }
            await outboxRepository.SaveChangesAsync(cancellationToken);
        }
    }
}
