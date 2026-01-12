using System.Text.Json;
using TinderClone.MatchingService.DTOs;

namespace TinderClone.MatchingService.Services;

public class UserServiceClient : IUserServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<UserServiceClient> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public UserServiceClient(HttpClient httpClient, ILogger<UserServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<UserDto?> GetUserByIdAsync(Guid userId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/users/{userId}");
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to get user {UserId}. Status: {StatusCode}", userId, response.StatusCode);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<UserDto>(content, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user {UserId}", userId);
            return null;
        }
    }

    public async Task<List<UserDto>> GetUsersByIdsAsync(List<Guid> userIds)
    {
        try
        {
            var idsString = string.Join(",", userIds);
            var response = await _httpClient.GetAsync($"/api/users/batch?ids={idsString}");
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to get users. Status: {StatusCode}", response.StatusCode);
                return new List<UserDto>();
            }

            var content = await response.Content.ReadAsStringAsync();
            var users = JsonSerializer.Deserialize<List<UserDto>>(content, _jsonOptions);
            return users ?? new List<UserDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting users batch");
            return new List<UserDto>();
        }
    }
}

