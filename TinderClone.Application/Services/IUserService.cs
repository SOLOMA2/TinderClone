using TinderClone.Domain.Entities;
using TinderClone.Domain.Enums;

namespace TinderClone.Application.Services;

public interface IUserService
{
    Task<User?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<User?> GetUserByIdWithPhotosAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<List<User>> GetRecommendationsAsync(
        Guid userId,
        double? maxDistance,
        int? minAge,
        int? maxAge,
        int count,
        CancellationToken cancellationToken = default);
}

