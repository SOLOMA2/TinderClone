using StackExchange.Redis;
using System.Text.Json;
using TinderClone.Domain.Entities;

namespace TinderClone.Infrastructure.Cache;

public class RedisCacheService : IRedisCacheService
{
    private readonly IDatabase _database;
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisCacheService> _logger;
    private const string UserKeyPrefix = "user:";
    private const string DeckKeyPrefix = "deck:";
    private readonly JsonSerializerOptions _jsonOptions;

    public RedisCacheService(IConnectionMultiplexer redis, ILogger<RedisCacheService> logger)
    {
        _redis = redis;
        _database = redis.GetDatabase();
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    // Cache-Aside для профилей
    public async Task<User?> GetCachedUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var key = $"{UserKeyPrefix}{userId}";
            var json = await _database.StringGetAsync(key);
            
            if (json.IsNullOrEmpty)
                return null;

            return JsonSerializer.Deserialize<User>(json!, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cached user {UserId}", userId);
            return null;
        }
    }

    public async Task CacheUserAsync(User user, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var key = $"{UserKeyPrefix}{user.Id}";
            var json = JsonSerializer.Serialize(user, _jsonOptions);
            await _database.StringSetAsync(key, json, expiration ?? TimeSpan.FromHours(24));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error caching user {UserId}", user.Id);
        }
    }

    public async Task InvalidateUserCacheAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var key = $"{UserKeyPrefix}{userId}";
            await _database.KeyDeleteAsync(key);
            
            // Также инвалидируем колоду, если пользователь изменил профиль/фильтры
            await InvalidateDeckCacheAsync(userId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating user cache {UserId}", userId);
        }
    }

    // Кеширование колоды (Deck)
    public async Task<List<Guid>?> GetCachedDeckAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var key = $"{DeckKeyPrefix}{userId}";
            var json = await _database.StringGetAsync(key);
            
            if (json.IsNullOrEmpty)
                return null;

            return JsonSerializer.Deserialize<List<Guid>>(json!, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cached deck for user {UserId}", userId);
            return null;
        }
    }

    public async Task CacheDeckAsync(Guid userId, List<Guid> userIds, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var key = $"{DeckKeyPrefix}{userId}";
            var json = JsonSerializer.Serialize(userIds, _jsonOptions);
            await _database.StringSetAsync(key, json, expiration ?? TimeSpan.FromMinutes(30));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error caching deck for user {UserId}", userId);
        }
    }

    public async Task InvalidateDeckCacheAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var key = $"{DeckKeyPrefix}{userId}";
            await _database.KeyDeleteAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating deck cache for user {UserId}", userId);
        }
    }

    public async Task<Guid?> PopFromDeckAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var deck = await GetCachedDeckAsync(userId, cancellationToken);
            if (deck == null || !deck.Any())
                return null;

            var nextUserId = deck.First();
            deck.RemoveAt(0);

            if (deck.Any())
            {
                await CacheDeckAsync(userId, deck, cancellationToken: cancellationToken);
            }
            else
            {
                await InvalidateDeckCacheAsync(userId, cancellationToken);
            }

            return nextUserId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error popping from deck for user {UserId}", userId);
            return null;
        }
    }
}

