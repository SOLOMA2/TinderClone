namespace TinderClone.MatchingService.DTOs;

public class RecommendationRequest
{
    public Guid UserId { get; set; }
    public int MaxDistance { get; set; } = 50; // в км
    public int? MinAge { get; set; }
    public int? MaxAge { get; set; }
    public int Count { get; set; } = 10;
}

