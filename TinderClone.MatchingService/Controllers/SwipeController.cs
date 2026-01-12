using Microsoft.AspNetCore.Mvc;
using TinderClone.MatchingService.DTOs;
using TinderClone.MatchingService.Services;

namespace TinderClone.MatchingService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SwipeController : ControllerBase
{
    private readonly IMatchingService _matchingService;
    private readonly ILogger<SwipeController> _logger;

    public SwipeController(IMatchingService matchingService, ILogger<SwipeController> logger)
    {
        _matchingService = matchingService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<SwipeResponse>> Swipe([FromBody] SwipeRequest request)
    {
        try
        {
            if (request.FromUserId == Guid.Empty || request.ToUserId == Guid.Empty)
            {
                return BadRequest("Invalid user IDs");
            }

            if (request.FromUserId == request.ToUserId)
            {
                return BadRequest("Cannot swipe on yourself");
            }

            var response = await _matchingService.ProcessSwipeAsync(request);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing swipe");
            return StatusCode(500, "Internal server error");
        }
    }
}

