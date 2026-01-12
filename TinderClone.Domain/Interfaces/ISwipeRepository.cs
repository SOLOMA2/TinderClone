using TinderClone.Domain.Entities;

namespace TinderClone.Domain.Interfaces;

public interface ISwipeRepository
{
    Task<Swipe?> GetSwipeAsync(Guid user1Id, Guid user2Id, CancellationToken cancellationToken = default);
    Task<Swipe> UpsertSwipeAsync(Guid fromUserId, Guid toUserId, bool isLike, bool isSuperLike, CancellationToken cancellationToken = default);
    Task<bool> IsMatchAsync(Guid user1Id, Guid user2Id, CancellationToken cancellationToken = default);
    Task<List<Guid>> GetSwipedUserIdsAsync(Guid userId, CancellationToken cancellationToken = default);
}
