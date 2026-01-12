using NetTopologySuite.Geometries;
using TinderClone.Domain.Entities;
using TinderClone.Domain.Enums;

namespace TinderClone.Domain.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<User?> GetByIdWithPhotosAsync(Guid id, CancellationToken cancellationToken = default);
    void Add(User user);

    /// <summary>
    /// Получает потенциальных матчей с учетом геолокации (PostGIS)
    /// </summary>
    Task<List<User>> GetPotentialMatchesAsync(
        Guid currentUserId,
        Gender preferredGender,
        int minAge,
        int maxAge,
        int count, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Получает пользователей в радиусе от указанной точки (PostGIS)
    /// </summary>
    Task<List<User>> GetUsersInRadiusAsync(
        Guid currentUserId,
        Point location,
        double radiusInKilometers,
        Gender preferredGender,
        int? minAge,
        int? maxAge,
        List<Guid> excludeUserIds,
        int count,
        CancellationToken cancellationToken = default);
}
