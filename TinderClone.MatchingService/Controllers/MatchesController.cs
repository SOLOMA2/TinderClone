using Microsoft.AspNetCore.Mvc;
using TinderClone.MatchingService.Services;

namespace TinderClone.MatchingService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MatchesController : ControllerBase
{
    private readonly IMatchingService _matchingService;
    private readonly ILogger<MatchesController> _logger;

    public MatchesController(IMatchingService matchingService, ILogger<MatchesController> logger)
    {
        _matchingService = matchingService;
        _logger = logger;
    }

    [HttpGet("{userId}")]
    public async Task<ActionResult<List<Guid>>> GetUserMatches(Guid userId)
    {
        try
        {
            if (userId == Guid.Empty)
            {
                return BadRequest("Invalid user ID");
            }

            var matches = await _matchingService.GetUserMatchesAsync(userId);
            return Ok(matches);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user matches");
            return StatusCode(500, "Internal server error");
        }
    }
}

