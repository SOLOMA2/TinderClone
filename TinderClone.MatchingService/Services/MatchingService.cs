using TinderClone.MatchingService.DTOs;

namespace TinderClone.MatchingService.Services;

public class MatchingService : IMatchingService
{
    private readonly IRedisCacheService _redisCache;
    private readonly IUserServiceClient _userServiceClient;
    private readonly IMessagePublisher _messagePublisher;
    private readonly ILogger<MatchingService> _logger;
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public MatchingService(
        IRedisCacheService redisCache,
        IUserServiceClient userServiceClient,
        IMessagePublisher messagePublisher,
        ILogger<MatchingService> logger,
        HttpClient httpClient,
        IConfiguration configuration)
    {
        _redisCache = redisCache;
        _userServiceClient = userServiceClient;
        _messagePublisher = messagePublisher;
        _logger = logger;
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public async Task<SwipeResponse> ProcessSwipeAsync(SwipeRequest request)
    {
        // Проверяем, не свайпал ли уже пользователь
        var alreadySwiped = await _redisCache.IsUserAlreadySwipedAsync(request.FromUserId, request.ToUserId);
        if (alreadySwiped)
        {
            return new SwipeResponse
            {
                IsMatch = false,
                Message = "Вы уже свайпали этого пользователя"
            };
        }

        // Кешируем свайп
        await _redisCache.CacheSwipeAsync(request.FromUserId, request.ToUserId, request.IsLike, request.IsSuperLike);

        // Отправляем событие о свайпе через RabbitMQ
        await _messagePublisher.PublishSwipeProcessedAsync(request.FromUserId, request.ToUserId, request.IsLike, request.IsSuperLike);

        // Если это лайк, проверяем взаимность
        if (request.IsLike)
        {
            var isReciprocalLike = await _redisCache.IsUserAlreadySwipedAsync(request.ToUserId, request.FromUserId);
            
            if (isReciprocalLike)
            {
                // Проверяем, был ли это лайк (не дизлайк)
                // Создаем матч
                var matchId = Guid.NewGuid();
                await _redisCache.CacheMatchAsync(matchId, request.FromUserId, request.ToUserId);

                // Отправляем событие о создании матча через RabbitMQ
                await _messagePublisher.PublishMatchCreatedAsync(matchId, request.FromUserId, request.ToUserId);

                // Увеличиваем скор пользователей
                await _redisCache.IncrementUserScoreAsync(request.FromUserId);
                await _redisCache.IncrementUserScoreAsync(request.ToUserId);

                // Удаляем пользователя из рекомендаций
                await _redisCache.RemoveUserFromRecommendationsAsync(request.FromUserId, request.ToUserId);
                await _redisCache.RemoveUserFromRecommendationsAsync(request.ToUserId, request.FromUserId);

                return new SwipeResponse
                {
                    IsMatch = true,
                    MatchId = matchId,
                    Message = "It's a match! 🎉"
                };
            }
        }

        // Удаляем пользователя из кеша рекомендаций
        await _redisCache.RemoveUserFromRecommendationsAsync(request.FromUserId, request.ToUserId);

        return new SwipeResponse
        {
            IsMatch = false,
            Message = request.IsLike ? "Лайк отправлен" : "Пропущено"
        };
    }

    public async Task<List<UserDto>> GetRecommendationsAsync(RecommendationRequest request)
    {
        // Сначала проверяем кеш
        var cachedRecommendations = await _redisCache.GetCachedUserRecommendationsAsync(request.UserId);
        if (cachedRecommendations != null && cachedRecommendations.Any())
        {
            _logger.LogInformation("Returning cached recommendations for user {UserId}", request.UserId);
            return cachedRecommendations.Take(request.Count).ToList();
        }

        // Получаем текущего пользователя
        var currentUser = await GetUserFromCacheOrApiAsync(request.UserId);
        if (currentUser == null)
        {
            _logger.LogWarning("User {UserId} not found", request.UserId);
            return new List<UserDto>();
        }

        // Получаем рекомендации из основного API
        var recommendations = await GetRecommendationsFromMainApiAsync(request);
        
        // Фильтруем уже свайпнутых
        var filteredRecommendations = new List<UserDto>();
        foreach (var recommendation in recommendations)
        {
            var alreadySwiped = await _redisCache.IsUserAlreadySwipedAsync(request.UserId, recommendation.Id);
            if (!alreadySwiped)
            {
                filteredRecommendations.Add(recommendation);
            }
        }

        // Кешируем рекомендации
        if (filteredRecommendations.Any())
        {
            await _redisCache.CacheUserRecommendationsAsync(request.UserId, filteredRecommendations);
        }

        return filteredRecommendations.Take(request.Count).ToList();
    }

    public async Task<List<Guid>> GetUserMatchesAsync(Guid userId)
    {
        return await _redisCache.GetUserMatchesAsync(userId);
    }

    private async Task<UserDto?> GetUserFromCacheOrApiAsync(Guid userId)
    {
        // Сначала проверяем Redis кеш
        var cachedUser = await _redisCache.GetCachedUserAsync(userId);
        if (cachedUser != null)
            return cachedUser;

        // Если нет в кеше, получаем из API
        var user = await _userServiceClient.GetUserByIdAsync(userId);
        if (user != null)
        {
            await _redisCache.CacheUserAsync(user);
        }

        return user;
    }

    private async Task<List<UserDto>> GetRecommendationsFromMainApiAsync(RecommendationRequest request)
    {
        try
        {
            var queryParams = $"?userId={request.UserId}&maxDistance={request.MaxDistance}&count={request.Count}";
            if (request.MinAge.HasValue) queryParams += $"&minAge={request.MinAge}";
            if (request.MaxAge.HasValue) queryParams += $"&maxAge={request.MaxAge}";

            var response = await _httpClient.GetAsync($"/api/users/recommendations{queryParams}");
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to get recommendations from main API. Status: {StatusCode}", response.StatusCode);
                return new List<UserDto>();
            }

            var content = await response.Content.ReadAsStringAsync();
            var options = new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            var recommendations = System.Text.Json.JsonSerializer.Deserialize<List<UserDto>>(content, options);
            
            return recommendations ?? new List<UserDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recommendations from main API");
            return new List<UserDto>();
        }
    }

}

