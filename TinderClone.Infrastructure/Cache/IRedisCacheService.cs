using TinderClone.Domain.Entities;

namespace TinderClone.Infrastructure.Cache;

/// <summary>
/// Сервис для кеширования в Redis
/// Реализует Cache-Aside паттерн
/// </summary>
public interface IRedisCacheService
{
    // Cache-Aside для профилей
    Task<User?> GetCachedUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task CacheUserAsync(User user, TimeSpan? expiration = null, CancellationToken cancellationToken = default);
    Task InvalidateUserCacheAsync(Guid userId, CancellationToken cancellationToken = default);

    // Кеширование колоды (Deck) - стопка кандидатов для пользователя
    Task<List<Guid>?> GetCachedDeckAsync(Guid userId, CancellationToken cancellationToken = default);
    Task CacheDeckAsync(Guid userId, List<Guid> userIds, TimeSpan? expiration = null, CancellationToken cancellationToken = default);
    Task InvalidateDeckCacheAsync(Guid userId, CancellationToken cancellationToken = default);
    
    // Получение следующего пользователя из колоды
    Task<Guid?> PopFromDeckAsync(Guid userId, CancellationToken cancellationToken = default);
}

