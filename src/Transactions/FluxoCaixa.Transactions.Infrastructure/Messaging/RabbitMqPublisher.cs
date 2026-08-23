using System.Text;
using FluxoCaixa.Transactions.Domain.Interfaces.Messaging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace FluxoCaixa.Transactions.Infrastructure.Messaging;

public sealed class RabbitMqPublisher : IMessageBus, IAsyncDisposable
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<RabbitMqPublisher> _logger;
    private readonly SemaphoreSlim _connectionLock = new(1, 1);
    private IConnection? _connection;

    private const string ExchangeName = "transactions-exchange";
    private const string QueueName = "transaction-created-queue";
    private const string RoutingKey = "transaction.created";

    public RabbitMqPublisher(IConfiguration configuration, ILogger<RabbitMqPublisher> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task PublishAsync(string messageType, string messageContent, CancellationToken cancellationToken = default)
    {
        await PublishBatchAsync(new[] { (messageType, messageContent) }, cancellationToken);
    }

    public async Task PublishBatchAsync(IEnumerable<(string MessageType, string MessageContent)> messages, CancellationToken cancellationToken = default)
    {
        var messageList = messages as IList<(string MessageType, string MessageContent)> ?? messages.ToList();
        if (messageList.Count == 0)
        {
            return;
        }

        var connection = await GetConnectionAsync(cancellationToken);
        using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);

        foreach (var (messageType, messageContent) in messageList)
        {
            var body = Encoding.UTF8.GetBytes(messageContent);
            var props = new BasicProperties
            {
                ContentType = "application/json",
                DeliveryMode = DeliveryModes.Persistent,
                Type = messageType
            };

            await channel.BasicPublishAsync(
                exchange: ExchangeName,
                routingKey: RoutingKey,
                mandatory: false,
                basicProperties: props,
                body: body,
                cancellationToken: cancellationToken);
        }
    }

    private async Task<IConnection> GetConnectionAsync(CancellationToken cancellationToken)
    {
        if (_connection is { IsOpen: true })
        {
            return _connection;
        }

        await _connectionLock.WaitAsync(cancellationToken);
        try
        {
            if (_connection is { IsOpen: true })
            {
                return _connection;
            }

            if (_connection is not null)
            {
                try
                {
                    await _connection.CloseAsync(cancellationToken: cancellationToken);
                    _connection.Dispose();
                }
                catch
                {
                    // Ignora limpeza de conexão quebrada anterior
                }
                _connection = null;
            }

            var host = _configuration["RabbitMQ:Host"] ?? "localhost";
            var username = _configuration["RabbitMQ:Username"] ?? "guest";
            var password = _configuration["RabbitMQ:Password"] ?? "guest";

            _logger.LogInformation("Estabelecendo nova conexão persistente com RabbitMQ em {Host}...", host);

            var factory = new ConnectionFactory
            {
                HostName = host,
                UserName = username,
                Password = password,
                AutomaticRecoveryEnabled = true,
                TopologyRecoveryEnabled = true
            };

            _connection = await factory.CreateConnectionAsync(cancellationToken);

            using var initChannel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);

            await initChannel.ExchangeDeclareAsync(
                exchange: ExchangeName,
                type: ExchangeType.Direct,
                durable: true,
                autoDelete: false,
                cancellationToken: cancellationToken);

            await initChannel.QueueDeclareAsync(
                queue: QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                cancellationToken: cancellationToken);

            await initChannel.QueueBindAsync(
                queue: QueueName,
                exchange: ExchangeName,
                routingKey: RoutingKey,
                cancellationToken: cancellationToken);

            _logger.LogInformation("Conexão com RabbitMQ estabelecida e topologia (Exchange: {Exchange}, Queue: {Queue}) declarada com sucesso.", ExchangeName, QueueName);

            return _connection;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao estabelecer conexão ou declarar topologia no RabbitMQ.");
            throw;
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
        {
            _logger.LogInformation("Encerrando conexão persistente com o RabbitMQ.");
            try
            {
                await _connection.CloseAsync();
                _connection.Dispose();
            }
            catch
            {
                // Ignora se já estiver desconectado
            }
        }

        _connectionLock.Dispose();
    }
}
