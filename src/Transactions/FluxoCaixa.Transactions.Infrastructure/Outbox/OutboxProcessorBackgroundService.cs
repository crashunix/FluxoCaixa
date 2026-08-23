using FluxoCaixa.Transactions.Domain.Interfaces.Messaging;
using FluxoCaixa.Transactions.Domain.Interfaces.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FluxoCaixa.Transactions.Infrastructure.Outbox;

public sealed class OutboxProcessorBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxProcessorBackgroundService> _logger;
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(1);

    public OutboxProcessorBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<OutboxProcessorBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Outbox Processor Background Service iniciado.");
        using var timer = new PeriodicTimer(Interval);

        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await ProcessOutboxMessagesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar mensagens da fila Outbox.");
            }
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

        _logger.LogInformation("Outbox Processor encontrou {Count} mensagem(ns) pendente(s).", messages.Count);

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
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao publicar lote de mensagens do Outbox no RabbitMQ.");
            foreach (var message in messages)
            {
                message.Error = ex.Message;
            }
        }

        await outboxRepository.SaveChangesAsync(cancellationToken);
    }
}
