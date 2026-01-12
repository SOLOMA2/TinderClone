namespace TinderClone.MatchingService.DTOs;

public class SwipeRequest
{
    public Guid FromUserId { get; set; }
    public Guid ToUserId { get; set; }
    public bool IsLike { get; set; }
    public bool IsSuperLike { get; set; }
}

