using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using FluxoCaixa.Consolidated.Domain.Repositories;
using FluxoCaixa.Shared.Events;
using Polly;
using Polly.Retry;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace FluxoCaixa.Consolidated.Worker.Consumers;

public sealed class TransactionCreatedConsumer : BackgroundService
{
    private static readonly ActivitySource ActivitySource = new("FluxoCaixa.Consolidated.Consumer");

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<TransactionCreatedConsumer> _logger;

    private const string ExchangeName = "transactions-exchange";
    private const string QueueName = "transaction-created-queue";
    private const string RoutingKey = "transaction.created";

    private sealed record ConsumedMessage(
        ulong DeliveryTag,
        byte[] Body,
        IDictionary<string, object?>? Headers
    );

    public TransactionCreatedConsumer(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<TransactionCreatedConsumer> logger)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var host = _configuration["RabbitMQ:Host"] ?? "rabbitmq";
        var username = _configuration["RabbitMQ:Username"] ?? "guest";
        var password = _configuration["RabbitMQ:Password"] ?? "guest";

        ushort batchSize = ushort.TryParse(_configuration["RabbitMQ:BatchSize"], out var bs) ? bs : (ushort)200;
        int batchTimeoutMs = int.TryParse(_configuration["RabbitMQ:BatchTimeoutMs"], out var bt) ? bt : 500;

        _logger.LogInformation("Consolidated Consumer iniciando escuta no RabbitMQ ({Host}) com Prefetch={BatchSize} e BatchTimeout={Timeout}ms...",
            host, batchSize, batchTimeoutMs);

        var factory = new ConnectionFactory
        {
            HostName = host,
            UserName = username,
            Password = password,
            AutomaticRecoveryEnabled = true,
            TopologyRecoveryEnabled = true
        };

        var pipeline = new ResiliencePipelineBuilder<IConnection>()
            .AddRetry(new RetryStrategyOptions<IConnection>
            {
                ShouldHandle = new PredicateBuilder<IConnection>()
                    .Handle<Exception>(ex => ex is not OperationCanceledException),
                MaxRetryAttempts = 10,
                Delay = TimeSpan.FromSeconds(2),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                OnRetry = args =>
                {
                    _logger.LogWarning(
                        args.Outcome.Exception,
                        "Tentativa {AttemptNumber} de conexão ao RabbitMQ falhou. Nova tentativa em {RetryDelay}...",
                        args.AttemptNumber + 1,
                        args.RetryDelay);
                    return ValueTask.CompletedTask;
                }
            })
            .Build();

        var connection = await pipeline.ExecuteAsync(
            async token => await factory.CreateConnectionAsync(token),
            stoppingToken);

        using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await channel.BasicQosAsync(
            prefetchSize: 0,
            prefetchCount: batchSize,
            global: false,
            cancellationToken: stoppingToken);

        await channel.ExchangeDeclareAsync(
            exchange: ExchangeName,
            type: ExchangeType.Direct,
            durable: true,
            autoDelete: false,
            cancellationToken: stoppingToken);

        await channel.QueueDeclareAsync(
            queue: QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: stoppingToken);

        await channel.QueueBindAsync(
            queue: QueueName,
            exchange: ExchangeName,
            routingKey: RoutingKey,
            cancellationToken: stoppingToken);

        var messageChannel = Channel.CreateBounded<ConsumedMessage>(new BoundedChannelOptions(batchSize * 2)
        {
            SingleReader = true,
            SingleWriter = false
        });

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (model, ea) =>
        {
            IDictionary<string, object?>? headers = ea.BasicProperties?.Headers is null
                ? null
                : new Dictionary<string, object?>(ea.BasicProperties.Headers);

            var msg = new ConsumedMessage(ea.DeliveryTag, ea.Body.ToArray(), headers);
            await messageChannel.Writer.WriteAsync(msg, stoppingToken);
        };

        await channel.BasicConsumeAsync(
            queue: QueueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        _logger.LogInformation("Consolidated Consumer conectado e aguardando eventos na fila {Queue}.", QueueName);

        while (!stoppingToken.IsCancellationRequested)
        {
            var batch = new List<ConsumedMessage>(batchSize);

            try
            {
                if (!await messageChannel.Reader.WaitToReadAsync(stoppingToken))
                {
                    break;
                }

                if (messageChannel.Reader.TryRead(out var firstItem))
                {
                    batch.Add(firstItem);
                }

                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                timeoutCts.CancelAfter(batchTimeoutMs);

                try
                {
                    while (batch.Count < batchSize && !timeoutCts.IsCancellationRequested)
                    {
                        if (messageChannel.Reader.TryRead(out var item))
                        {
                            batch.Add(item);
                        }
                        else
                        {
                            var hasMore = await messageChannel.Reader.WaitToReadAsync(timeoutCts.Token);
                            if (!hasMore) break;
                        }
                    }
                }
                catch (OperationCanceledException) when (!stoppingToken.IsCancellationRequested)
                {
                    // Window timeout reached for collecting batch
                }

                if (batch.Count > 0)
                {
                    await ProcessBatchAsync(channel, batch, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro no loop principal de consumo em lote.");
                await Task.Delay(1000, stoppingToken);
            }
        }
    }

    private async Task ProcessBatchAsync(IChannel channel, List<ConsumedMessage> batch, CancellationToken cancellationToken)
    {
        if (batch.Count == 0)
        {
            return;
        }

        var maxDeliveryTag = batch.Max(x => x.DeliveryTag);

        var validItems = new List<(ConsumedMessage Msg, TransactionCreatedEvent Event, ActivityContext? TraceContext)>();
        var invalidItems = new List<ConsumedMessage>();

        foreach (var msg in batch)
        {
            try
            {
                var eventData = JsonSerializer.Deserialize<TransactionCreatedEvent>(msg.Body);

                if (eventData is not null && eventData.Id != Guid.Empty)
                {
                    var traceCtx = ExtractTraceContext(msg);
                    validItems.Add((msg, eventData, traceCtx));
                }
                else
                {
                    _logger.LogError("Payload com estrutura invalida ou Id vazio recebido.");
                    invalidItems.Add(msg);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao deserializar mensagem JSON.");
                invalidItems.Add(msg);
            }
        }

        if (invalidItems.Count > 0)
        {
            _logger.LogError("{Count} mensagens com payload invalido ignoradas no lote. Executando Nack sem requeue.", invalidItems.Count);
            foreach (var invalidMsg in invalidItems)
            {
                try
                {
                    await channel.BasicNackAsync(invalidMsg.DeliveryTag, multiple: false, requeue: false, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao enviar Nack para mensagem invalida {Tag}.", invalidMsg.DeliveryTag);
                }
            }
        }

        if (validItems.Count == 0)
        {
            _logger.LogInformation("Nenhuma mensagem valida no lote de {Count} mensagens. Executando Ack em lote.", batch.Count);
            await channel.BasicAckAsync(maxDeliveryTag, multiple: true, cancellationToken);
            return;
        }

        var links = new List<ActivityLink>();
        foreach (var item in validItems)
        {
            if (item.TraceContext.HasValue)
            {
                links.Add(new ActivityLink(item.TraceContext.Value));
            }
        }

        var primaryContext = validItems.FirstOrDefault(x => x.TraceContext.HasValue).TraceContext;

        using var activity = primaryContext.HasValue
            ? ActivitySource.StartActivity(
                "Process TransactionCreatedEvents Batch",
                ActivityKind.Consumer,
                primaryContext.Value,
                tags: null,
                links: links.Count > 0 ? links : null)
            : ActivitySource.StartActivity(
                "Process TransactionCreatedEvents Batch",
                ActivityKind.Consumer,
                default(ActivityContext),
                tags: null,
                links: links.Count > 0 ? links : null);

        activity?.SetTag("messaging.batch.message_count", batch.Count);

        // deduplicação
        var distinctEvents = validItems
            .GroupBy(x => x.Event.Id)
            .Select(g => g.First())
            .ToList();

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IDailyBalanceRepository>();

            var eventsToProcess = distinctEvents.Select(x => x.Event).ToList();
            await repository.ProcessBatchAsync(eventsToProcess, cancellationToken);

            _logger.LogInformation("Lote de {TotalCount} mensagens processado no banco.", validItems.Count);

            await channel.BasicAckAsync(maxDeliveryTag, multiple: true, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar lote de {Count} mensagens no banco de dados. Reenfileirando (Nack).", batch.Count);
            await channel.BasicNackAsync(maxDeliveryTag, multiple: true, requeue: true, cancellationToken);
        }
    }

    private static ActivityContext? ExtractTraceContext(ConsumedMessage msg)
    {
        if (msg.Headers is null)
            return null;

        string? traceparent = GetHeaderValue(msg.Headers, "traceparent");
        if (!string.IsNullOrWhiteSpace(traceparent) && ActivityContext.TryParse(traceparent, null, out var parsedTraceparent))
        {
            return parsedTraceparent;
        }

        string? traceId = GetHeaderValue(msg.Headers, "traceId");
        string? spanId = GetHeaderValue(msg.Headers, "spanId");

        if (!string.IsNullOrWhiteSpace(traceId) && !string.IsNullOrWhiteSpace(spanId))
        {
            try
            {
                return new ActivityContext(
                    ActivityTraceId.CreateFromString(traceId),
                    ActivitySpanId.CreateFromString(spanId),
                    ActivityTraceFlags.Recorded);
            }
            catch
            {
                return null;
            }
        }

        return null;
    }

    private static string? GetHeaderValue(IDictionary<string, object?> headers, string key)
    {
        if (headers.TryGetValue(key, out var val) && val is not null)
        {
            if (val is byte[] bytes)
                return Encoding.UTF8.GetString(bytes);
            if (val is string str)
                return str;
            if (val is ReadOnlyMemory<byte> rom)
                return Encoding.UTF8.GetString(rom.Span);
        }
        return null;
    }
}
