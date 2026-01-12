using StackExchange.Redis;
using System.Text.Json;
using TinderClone.MatchingService.DTOs;

namespace TinderClone.MatchingService.Services;

public class RedisCacheService : IRedisCacheService
{
    private readonly IDatabase _database;
    private readonly IConnectionMultiplexer _redis;
    private const string UserKeyPrefix = "user:";
    private const string RecommendationsKeyPrefix = "recommendations:";
    private const string SwipeKeyPrefix = "swipe:";
    private const string MatchKeyPrefix = "match:";
    private const string UserMatchesKeyPrefix = "usermatches:";
    private const string UserScoreKeyPrefix = "userscore:";

    public RedisCacheService(IConnectionMultiplexer redis)
    {
        _redis = redis;
        _database = redis.GetDatabase();
    }

    public async Task CacheUserAsync(UserDto user, TimeSpan? expiration = null)
    {
        var key = $"{UserKeyPrefix}{user.Id}";
        var json = JsonSerializer.Serialize(user);
        await _database.StringSetAsync(key, json, expiration ?? TimeSpan.FromHours(24));
    }

    public async Task<UserDto?> GetCachedUserAsync(Guid userId)
    {
        var key = $"{UserKeyPrefix}{userId}";
        var json = await _database.StringGetAsync(key);
        
        if (json.IsNullOrEmpty)
            return null;

        return JsonSerializer.Deserialize<UserDto>(json!);
    }

    public async Task CacheUserRecommendationsAsync(Guid userId, List<UserDto> recommendations, TimeSpan? expiration = null)
    {
        var key = $"{RecommendationsKeyPrefix}{userId}";
        var json = JsonSerializer.Serialize(recommendations);
        await _database.StringSetAsync(key, json, expiration ?? TimeSpan.FromMinutes(30));
    }

    public async Task<List<UserDto>?> GetCachedUserRecommendationsAsync(Guid userId)
    {
        var key = $"{RecommendationsKeyPrefix}{userId}";
        var json = await _database.StringGetAsync(key);
        
        if (json.IsNullOrEmpty)
            return null;

        return JsonSerializer.Deserialize<List<UserDto>>(json!);
    }

    public async Task CacheSwipeAsync(Guid fromUserId, Guid toUserId, bool isLike, bool isSuperLike)
    {
        var key = $"{SwipeKeyPrefix}{fromUserId}:{toUserId}";
        var swipeData = new { IsLike = isLike, IsSuperLike = isSuperLike, Timestamp = DateTime.UtcNow };
        var json = JsonSerializer.Serialize(swipeData);
        await _database.StringSetAsync(key, json, TimeSpan.FromDays(90));
    }

    public async Task<bool> IsUserAlreadySwipedAsync(Guid fromUserId, Guid toUserId)
    {
        var key = $"{SwipeKeyPrefix}{fromUserId}:{toUserId}";
        return await _database.KeyExistsAsync(key);
    }

    public async Task CacheMatchAsync(Guid matchId, Guid userAId, Guid userBId)
    {
        var matchKey = $"{MatchKeyPrefix}{matchId}";
        var matchData = new { MatchId = matchId, UserAId = userAId, UserBId = userBId, MatchedAt = DateTime.UtcNow };
        var json = JsonSerializer.Serialize(matchData);
        await _database.StringSetAsync(matchKey, json, TimeSpan.FromDays(365));

        // Добавляем матч в списки пользователей
        var userAKey = $"{UserMatchesKeyPrefix}{userAId}";
        var userBKey = $"{UserMatchesKeyPrefix}{userBId}";
        await _database.SetAddAsync(userAKey, matchId.ToString());
        await _database.SetAddAsync(userBKey, matchId.ToString());
        await _database.KeyExpireAsync(userAKey, TimeSpan.FromDays(365));
        await _database.KeyExpireAsync(userBKey, TimeSpan.FromDays(365));
    }

    public async Task<List<Guid>> GetUserMatchesAsync(Guid userId)
    {
        var key = $"{UserMatchesKeyPrefix}{userId}";
        var members = await _database.SetMembersAsync(key);
        return members.Select(m => Guid.Parse(m!)).ToList();
    }

    public async Task RemoveUserFromRecommendationsAsync(Guid userId, Guid recommendedUserId)
    {
        var key = $"{RecommendationsKeyPrefix}{userId}";
        var recommendations = await GetCachedUserRecommendationsAsync(userId);
        
        if (recommendations != null)
        {
            recommendations.RemoveAll(r => r.Id == recommendedUserId);
            await CacheUserRecommendationsAsync(userId, recommendations);
        }
    }

    public async Task IncrementUserScoreAsync(Guid userId)
    {
        var key = $"{UserScoreKeyPrefix}{userId}";
        await _database.StringIncrementAsync(key);
        await _database.KeyExpireAsync(key, TimeSpan.FromDays(30));
    }

    public async Task<double> GetUserScoreAsync(Guid userId)
    {
        var key = $"{UserScoreKeyPrefix}{userId}";
        var value = await _database.StringGetAsync(key);
        return value.HasValue ? (double)value! : 0;
    }
}

