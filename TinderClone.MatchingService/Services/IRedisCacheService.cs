using TinderClone.MatchingService.DTOs;

namespace TinderClone.MatchingService.Services;

public interface IRedisCacheService
{
    Task CacheUserAsync(UserDto user, TimeSpan? expiration = null);
    Task<UserDto?> GetCachedUserAsync(Guid userId);
    Task CacheUserRecommendationsAsync(Guid userId, List<UserDto> recommendations, TimeSpan? expiration = null);
    Task<List<UserDto>?> GetCachedUserRecommendationsAsync(Guid userId);
    Task CacheSwipeAsync(Guid fromUserId, Guid toUserId, bool isLike, bool isSuperLike);
    Task<bool> IsUserAlreadySwipedAsync(Guid fromUserId, Guid toUserId);
    Task CacheMatchAsync(Guid matchId, Guid userAId, Guid userBId);
    Task<List<Guid>> GetUserMatchesAsync(Guid userId);
    Task RemoveUserFromRecommendationsAsync(Guid userId, Guid recommendedUserId);
    Task IncrementUserScoreAsync(Guid userId);
    Task<double> GetUserScoreAsync(Guid userId);
}

