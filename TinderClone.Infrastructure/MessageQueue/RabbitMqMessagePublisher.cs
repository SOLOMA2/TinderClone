using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace TinderClone.Infrastructure.MessageQueue;

public class RabbitMqMessagePublisher : IMessagePublisher, IDisposable
{
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly ILogger<RabbitMqMessagePublisher> _logger;
    private const string MatchCreatedExchange = "matches.created";
    private const string UserUpdatedExchange = "users.updated";

    public RabbitMqMessagePublisher(IConfiguration configuration, ILogger<RabbitMqMessagePublisher> logger)
    {
        _logger = logger;
        var rabbitMqHost = configuration["RabbitMQ:HostName"] ?? "localhost";
        var factory = new ConnectionFactory
        {
            HostName = rabbitMqHost,
            Port = int.Parse(configuration["RabbitMQ:Port"] ?? "5672"),
            UserName = configuration["RabbitMQ:UserName"] ?? "guest",
            Password = configuration["RabbitMQ:Password"] ?? "guest"
        };

        try
        {
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            // Declare exchanges
            _channel.ExchangeDeclare(MatchCreatedExchange, ExchangeType.Topic, durable: true);
            _channel.ExchangeDeclare(UserUpdatedExchange, ExchangeType.Topic, durable: true);

            _logger.LogInformation("RabbitMQ connection established");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to RabbitMQ");
            throw;
        }
    }

    public Task PublishMatchCreatedAsync(Guid matchId, Guid userAId, Guid userBId)
    {
        try
        {
            var message = new
            {
                MatchId = matchId,
                UserAId = userAId,
                UserBId = userBId,
                CreatedAt = DateTime.UtcNow
            };

            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
            var properties = _channel.CreateBasicProperties();
            properties.Persistent = true;
            properties.MessageId = matchId.ToString();
            properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

            _channel.BasicPublish(
                exchange: MatchCreatedExchange,
                routingKey: "match.created",
                basicProperties: properties,
                body: body);

            _logger.LogInformation("Published match created event: MatchId={MatchId}, UserA={UserAId}, UserB={UserBId}", 
                matchId, userAId, userBId);

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish match created event");
            return Task.FromException(ex);
        }
    }

    public Task PublishUserUpdatedAsync(Guid userId)
    {
        try
        {
            var message = new
            {
                UserId = userId,
                UpdatedAt = DateTime.UtcNow
            };

            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
            var properties = _channel.CreateBasicProperties();
            properties.Persistent = true;
            properties.MessageId = Guid.NewGuid().ToString();
            properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

            _channel.BasicPublish(
                exchange: UserUpdatedExchange,
                routingKey: "user.updated",
                basicProperties: properties,
                body: body);

            _logger.LogInformation("Published user updated event: UserId={UserId}", userId);

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish user updated event");
            return Task.FromException(ex);
        }
    }

    public void Dispose()
    {
        _channel?.Close();
        _channel?.Dispose();
        _connection?.Close();
        _connection?.Dispose();
    }
}

