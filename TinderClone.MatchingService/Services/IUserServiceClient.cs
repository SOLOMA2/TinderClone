using TinderClone.MatchingService.DTOs;

namespace TinderClone.MatchingService.Services;

public interface IUserServiceClient
{
    Task<UserDto?> GetUserByIdAsync(Guid userId);
    Task<List<UserDto>> GetUsersByIdsAsync(List<Guid> userIds);
}

