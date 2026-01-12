namespace TinderClone.MatchingService.DTOs;

public class UserDto
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Bio { get; set; } = string.Empty;
    public int Age { get; set; }
    public int Gender { get; set; }
    public int PreferredGender { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public List<string> PhotoUrls { get; set; } = new();
    public DateTime LastActive { get; set; }
}

