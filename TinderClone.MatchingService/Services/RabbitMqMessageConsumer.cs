using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using TinderClone.MatchingService.DTOs;

namespace TinderClone.MatchingService.Services;

public class RabbitMqMessageConsumer : BackgroundService
{
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly IRedisCacheService _redisCache;
    private readonly ILogger<RabbitMqMessageConsumer> _logger;
    private const string UserUpdatedExchange = "users.updated";
    private const string UserUpdatedQueue = "matching_service_user_updated";

    public RabbitMqMessageConsumer(
        IConfiguration configuration,
        IRedisCacheService redisCache,
        ILogger<RabbitMqMessageConsumer> logger)
    {
        _redisCache = redisCache;
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

            // Declare exchange
            _channel.ExchangeDeclare(UserUpdatedExchange, ExchangeType.Topic, durable: true);

            // Declare queue
            _channel.QueueDeclare(
                queue: UserUpdatedQueue,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            // Bind queue to exchange
            _channel.QueueBind(
                queue: UserUpdatedQueue,
                exchange: UserUpdatedExchange,
                routingKey: "user.updated");

            _logger.LogInformation("RabbitMQ consumer initialized");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize RabbitMQ consumer");
            throw;
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += async (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            var routingKey = ea.RoutingKey;

            try
            {
                await ProcessMessageAsync(message, routingKey);
                _channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message: {Message}", message);
                _channel.BasicNack(deliveryTag: ea.DeliveryTag, multiple: false, requeue: true);
            }
        };

        _channel.BasicConsume(
            queue: UserUpdatedQueue,
            autoAck: false,
            consumer: consumer);

        _logger.LogInformation("RabbitMQ consumer started");

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
    }

    private async Task ProcessMessageAsync(string message, string routingKey)
    {
        _logger.LogInformation("Received message: RoutingKey={RoutingKey}, Message={Message}", routingKey, message);

        if (routingKey == "user.updated")
        {
            var userDto = JsonSerializer.Deserialize<UserDto>(message, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (userDto != null)
            {
                // Обновляем кеш пользователя в Redis
                await _redisCache.CacheUserAsync(userDto);
                _logger.LogInformation("User cache updated: UserId={UserId}", userDto.Id);
            }
        }
    }

    public override void Dispose()
    {
        _channel?.Close();
        _channel?.Dispose();
        _connection?.Close();
        _connection?.Dispose();
        base.Dispose();
    }
}

