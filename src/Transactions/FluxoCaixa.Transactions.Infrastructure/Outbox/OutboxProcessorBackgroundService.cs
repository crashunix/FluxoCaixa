using FluxoCaixa.Transactions.Domain.Interfaces.Messaging;
using FluxoCaixa.Transactions.Domain.Interfaces.Repositories;
using FluxoCaixa.Transactions.Infrastructure.Context;
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
        var dbContext = scope.ServiceProvider.GetRequiredService<TransactionsDbContext>();
        var outboxRepository = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        const int maxRetries = 5;
        const int batchSize = 100;

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var messages = await outboxRepository.GetUnprocessedMessagesAsync(batchSize, maxRetries, cancellationToken);

        if (messages.Count == 0)
        {
            await transaction.CommitAsync(cancellationToken);
            return;
        }

        try
        {
            var batchPayloads = messages.Select(m => (m.Type, m.Content, m.TraceId, m.SpanId));

            await messageBus.PublishBatchAsync(batchPayloads, cancellationToken);

            var now = DateTime.UtcNow;
            foreach (var message in messages)
            {
                message.ProcessedOnUtc = now;
                message.Error = null;
            }

            await outboxRepository.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            _logger.LogInformation("Lote de {Count} mensagem(ns) do Outbox publicado no RabbitMQ e atualizado no banco.", messages.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao publicar lote de {Count} mensagem(ns) do Outbox no RabbitMQ. Incrementando RetryCount.", messages.Count);
            foreach (var message in messages)
            {
                message.RetryCount++;
                message.Error = ex.Message;
                if (message.RetryCount >= maxRetries)
                {
                    _logger.LogWarning("Mensagem Outbox {Id} atingiu o limite máximo de retries ({MaxRetries}) e não será mais reprocessada automaticamente.", message.Id, maxRetries);
                }
            }

            await outboxRepository.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
    }
}
