namespace TinderClone.MatchingService.Services;

public interface IMessagePublisher
{
    Task PublishMatchCreatedAsync(Guid matchId, Guid userAId, Guid userBId);
    Task PublishSwipeProcessedAsync(Guid fromUserId, Guid toUserId, bool isLike, bool isSuperLike);
}

