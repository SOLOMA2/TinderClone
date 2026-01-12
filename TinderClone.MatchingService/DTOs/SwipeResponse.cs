namespace TinderClone.MatchingService.DTOs;

public class SwipeResponse
{
    public bool IsMatch { get; set; }
    public Guid? MatchId { get; set; }
    public string Message { get; set; } = string.Empty;
}

