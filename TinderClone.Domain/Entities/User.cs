using NetTopologySuite.Geometries;
using TinderClone.Domain.Common;
using TinderClone.Domain.Enums;

namespace TinderClone.Domain.Entities;

public class User : BaseEntity
{
    private readonly List<UserPhoto> _photos = new();
    
    private readonly List<Match> _matchesAsUserA = new();
    private readonly List<Match> _matchesAsUserB = new();

    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string Bio { get; private set; } = string.Empty;
    public DateTime BirthDate { get; private set; }
    public Gender Gender { get; private set; }
    public Gender PreferredGender { get; private set; }

    // PostGIS: Используем Point для геолокации вместо double Latitude/Longitude
    public Point Location { get; private set; } = null!;
    public DateTime LastActive { get; private set; } = DateTime.UtcNow;

    // Вспомогательные свойства для удобства (не сохраняются в БД)
    public double Latitude => Location?.Y ?? 0;
    public double Longitude => Location?.X ?? 0;

    public virtual IReadOnlyCollection<UserPhoto> Photos => _photos.AsReadOnly();

    public virtual IReadOnlyCollection<Match> Matches => _matchesAsUserA.Concat(_matchesAsUserB).ToList().AsReadOnly();

    private User() { } 

    public User(string firstName, string lastName, DateTime birthDate, Gender gender, Gender preferredGender, double lat, double lon)
    {
        if (string.IsNullOrWhiteSpace(firstName)) throw new ArgumentException("Имя обязательно");
        if (birthDate > DateTime.UtcNow.AddYears(-18)) throw new ArgumentException("Регистрация разрешена только с 18 лет");

        FirstName = firstName;
        LastName = lastName;
        BirthDate = birthDate;
        Gender = gender;
        PreferredGender = preferredGender;
        Location = new Point(lon, lat) { SRID = 4326 }; // WGS84
        LastActive = DateTime.UtcNow;
    }

    public void UpdateProfile(string bio, Gender preferredGender)
    {
        Bio = bio?.Trim() ?? string.Empty;
        PreferredGender = preferredGender;
        LastActive = DateTime.UtcNow;
    }

    public void UpdateLocation(double lat, double lon)
    {
        Location = new Point(lon, lat) { SRID = 4326 }; // WGS84
        LastActive = DateTime.UtcNow;
    }

    public void AddPhoto(string url, bool isMain = false)
    {
        if (!_photos.Any()) isMain = true;

        if (isMain)
        {
            _photos.ForEach(p => p.SetMainStatus(false));
        }

        _photos.Add(new UserPhoto(url, isMain));
    }

    public void SetMainPhoto(Guid photoId)
    {
        var photo = _photos.FirstOrDefault(p => p.Id == photoId);
        if (photo == null) throw new InvalidOperationException("Фото не найдено");

        _photos.ForEach(p => p.SetMainStatus(false));
        photo.SetMainStatus(true);
    }

    public int GetAge()
    {
        var today = DateTime.Today;
        var age = today.Year - BirthDate.Year;
        if (BirthDate.Date > today.AddYears(-age)) age--;
        return age;
    }
}
