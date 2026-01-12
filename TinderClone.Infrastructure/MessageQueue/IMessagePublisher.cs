namespace TinderClone.Infrastructure.MessageQueue;

/// <summary>
/// Сервис для публикации событий через RabbitMQ
/// </summary>
public interface IMessagePublisher
{
    Task PublishMatchCreatedAsync(Guid matchId, Guid userAId, Guid userBId);
    Task PublishUserUpdatedAsync(Guid userId);
}

