using TinderClone.MatchingService.DTOs;

namespace TinderClone.MatchingService.Services;

public interface IMatchingService
{
    Task<SwipeResponse> ProcessSwipeAsync(SwipeRequest request);
    Task<List<UserDto>> GetRecommendationsAsync(RecommendationRequest request);
    Task<List<Guid>> GetUserMatchesAsync(Guid userId);
}

