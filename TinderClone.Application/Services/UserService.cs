using NetTopologySuite.Geometries;
using TinderClone.Domain.Entities;
using TinderClone.Domain.Enums;
using TinderClone.Domain.Interfaces;
using TinderClone.Infrastructure.Cache;

namespace TinderClone.Application.Services;

/// <summary>
/// Сервис для работы с пользователями
/// Реализует Cache-Aside паттерн для профилей
/// Управляет кешированием колоды (Deck)
/// </summary>
public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IRedisCacheService _cacheService;
    private readonly ILogger<UserService> _logger;

    public UserService(
        IUserRepository userRepository,
        IRedisCacheService cacheService,
        ILogger<UserService> logger)
    {
        _userRepository = userRepository;
        _cacheService = cacheService;
        _logger = logger;
    }

    /// <summary>
    /// Получает пользователя по ID с использованием Cache-Aside паттерна
    /// </summary>
    public async Task<User?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        // 1. Проверяем кеш (Cache-Aside)
        var cachedUser = await _cacheService.GetCachedUserAsync(userId, cancellationToken);
        if (cachedUser != null)
        {
            _logger.LogDebug("User {UserId} found in cache", userId);
            return cachedUser;
        }

        // 2. Если нет в кеше, получаем из БД
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            return null;
        }

        // 3. Сохраняем в кеш
        await _cacheService.CacheUserAsync(user, cancellationToken: cancellationToken);
        
        return user;
    }

    public async Task<User?> GetUserByIdWithPhotosAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        // Для пользователя с фото используем прямой запрос (фото могут быть большими)
        return await _userRepository.GetByIdWithPhotosAsync(userId, cancellationToken);
    }

    /// <summary>
    /// Получает рекомендации с использованием кеширования колоды
    /// </summary>
    public async Task<List<User>> GetRecommendationsAsync(
        Guid userId,
        double? maxDistance,
        int? minAge,
        int? maxAge,
        int count,
        CancellationToken cancellationToken = default)
    {
        // 1. Проверяем кеш колоды
        var cachedDeck = await _cacheService.GetCachedDeckAsync(userId, cancellationToken);
        List<Guid> deckUserIds;

        if (cachedDeck != null && cachedDeck.Count >= count)
        {
            // Используем кешированную колоду
            deckUserIds = cachedDeck.Take(count).ToList();
            _logger.LogDebug("Using cached deck for user {UserId}, {Count} users", userId, deckUserIds.Count);
        }
        else
        {
            // Формируем новую колоду
            var currentUser = await GetUserByIdAsync(userId, cancellationToken);
            if (currentUser == null)
            {
                _logger.LogWarning("User {UserId} not found", userId);
                return new List<User>();
            }

            List<User> recommendations;

            if (maxDistance.HasValue && currentUser.Location != null)
            {
                // Используем гео-запрос
                recommendations = await _userRepository.GetUsersInRadiusAsync(
                    userId,
                    currentUser.Location,
                    maxDistance.Value,
                    currentUser.PreferredGender,
                    minAge,
                    maxAge,
                    new List<Guid>(), // excludeUserIds - уже исключаются в репозитории
                    count * 2, // Берем больше для формирования колоды
                    cancellationToken);
            }
            else
            {
                // Используем обычный запрос
                var minAgeValue = minAge ?? 18;
                var maxAgeValue = maxAge ?? 100;
                recommendations = await _userRepository.GetPotentialMatchesAsync(
                    userId,
                    currentUser.PreferredGender,
                    minAgeValue,
                    maxAgeValue,
                    count * 2,
                    cancellationToken);
            }

            deckUserIds = recommendations.Select(u => u.Id).ToList();

            // Кешируем колоду
            if (deckUserIds.Any())
            {
                await _cacheService.CacheDeckAsync(userId, deckUserIds, cancellationToken: cancellationToken);
            }
        }

        // Загружаем пользователей из колоды
        var users = new List<User>();
        foreach (var deckUserId in deckUserIds.Take(count))
        {
            var user = await GetUserByIdWithPhotosAsync(deckUserId, cancellationToken);
            if (user != null)
            {
                users.Add(user);
            }
        }

        return users;
    }
}

