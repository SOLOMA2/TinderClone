using Microsoft.AspNetCore.Mvc;
using TinderClone.MatchingService.DTOs;
using TinderClone.MatchingService.Services;

namespace TinderClone.MatchingService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RecommendationsController : ControllerBase
{
    private readonly IMatchingService _matchingService;
    private readonly ILogger<RecommendationsController> _logger;

    public RecommendationsController(IMatchingService matchingService, ILogger<RecommendationsController> logger)
    {
        _matchingService = matchingService;
        _logger = logger;
    }

    [HttpPost("get")]
    public async Task<ActionResult<List<UserDto>>> GetRecommendations([FromBody] RecommendationRequest request)
    {
        try
        {
            if (request.UserId == Guid.Empty)
            {
                return BadRequest("Invalid user ID");
            }

            var recommendations = await _matchingService.GetRecommendationsAsync(request);
            return Ok(recommendations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recommendations");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("{userId}")]
    public async Task<ActionResult<List<UserDto>>> GetRecommendations(
        Guid userId,
        [FromQuery] int maxDistance = 50,
        [FromQuery] int? minAge = null,
        [FromQuery] int? maxAge = null,
        [FromQuery] int count = 10)
    {
        try
        {
            if (userId == Guid.Empty)
            {
                return BadRequest("Invalid user ID");
            }

            var request = new RecommendationRequest
            {
                UserId = userId,
                MaxDistance = maxDistance,
                MinAge = minAge,
                MaxAge = maxAge,
                Count = count
            };

            var recommendations = await _matchingService.GetRecommendationsAsync(request);
            return Ok(recommendations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recommendations");
            return StatusCode(500, "Internal server error");
        }
    }
}

