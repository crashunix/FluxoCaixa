using System.Text;
using FluxoCaixa.Transactions.Domain.Interfaces.Messaging;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;

namespace FluxoCaixa.Transactions.Infrastructure.Messaging;

public sealed class RabbitMqPublisher : IMessageBus, IAsyncDisposable
{
    private readonly IConfiguration _configuration;
    private readonly SemaphoreSlim _connectionLock = new(1, 1);
    private IConnection? _connection;

    private const string ExchangeName = "transactions-exchange";
    private const string QueueName = "transaction-created-queue";
    private const string RoutingKey = "transaction.created";

    public RabbitMqPublisher(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task PublishAsync(string messageType, string messageContent, CancellationToken cancellationToken = default)
    {
        await PublishBatchAsync(new[] { (messageType, messageContent) }, cancellationToken);
    }

    public async Task PublishBatchAsync(IEnumerable<(string MessageType, string MessageContent)> messages, CancellationToken cancellationToken = default)
    {
        var connection = await GetConnectionAsync(cancellationToken);
        using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);

        foreach (var (messageType, messageContent) in messages)
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

            var host = _configuration["RabbitMQ:Host"] ?? "localhost";
            var username = _configuration["RabbitMQ:Username"] ?? "guest";
            var password = _configuration["RabbitMQ:Password"] ?? "guest";

            var factory = new ConnectionFactory
            {
                HostName = host,
                UserName = username,
                Password = password
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

            return _connection;
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
            await _connection.CloseAsync();
            _connection.Dispose();
        }

        _connectionLock.Dispose();
    }
}
