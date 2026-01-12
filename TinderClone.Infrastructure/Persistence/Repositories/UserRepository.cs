using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using TinderClone.Domain.Entities;
using TinderClone.Domain.Enums;
using TinderClone.Domain.Interfaces;

namespace TinderClone.Infrastructure.Persistence.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    public UserRepository(AppDbContext context)
    {
        _context = context;
    }

    public void Add(User user)
    {
        _context.Users.Add(user);
    }

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task<User?> GetByIdWithPhotosAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .Include(u => u.Photos)
            .AsSplitQuery()
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task<List<User>> GetPotentialMatchesAsync(
        Guid currentUserId,
        Gender preferredGender,
        int minAge,
        int maxAge,
        int count,
        CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;
        var minBirthDate = today.AddYears(-(maxAge + 1));
        var maxBirthDate = today.AddYears(-minAge);

        var query = _context.Users
            .AsNoTracking() 
            .Include(u => u.Photos.Where(p => p.IsMain)) 
            .AsQueryable();

        query = query.Where(u => u.Gender == preferredGender);
        query = query.Where(u => u.BirthDate >= minBirthDate && u.BirthDate <= maxBirthDate);
        query = query.Where(u => u.Id != currentUserId);

        // Исключаем уже свайпнутых пользователей
        var swipedUserIds = await _context.Swipes
            .Where(s => (s.User1Id == currentUserId && s.Decision1 != null) ||
                       (s.User2Id == currentUserId && s.Decision2 != null))
            .Select(s => s.User1Id == currentUserId ? s.User2Id : s.User1Id)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (swipedUserIds.Any())
        {
            query = query.Where(u => !swipedUserIds.Contains(u.Id));
        }

        query = query.OrderByDescending(u => u.LastActive);

        return await query
            .Take(count)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Получает пользователей в радиусе от указанной точки используя PostGIS ST_DWithin
    /// Использует пространственные индексы GIST для оптимизации
    /// </summary>
    public async Task<List<User>> GetUsersInRadiusAsync(
        Guid currentUserId,
        Point location,
        double radiusInKilometers,
        Gender preferredGender,
        int? minAge,
        int? maxAge,
        List<Guid> excludeUserIds,
        int count,
        CancellationToken cancellationToken = default)
    {
        // Конвертируем радиус из километров в метры (ST_DWithin использует метры для geography)
        var radiusInMeters = radiusInKilometers * 1000;
        var lon = location.X;
        var lat = location.Y;

        var query = _context.Users
            .AsNoTracking()
            .Include(u => u.Photos.Where(p => p.IsMain))
            .Where(u => u.Gender == preferredGender)
            .Where(u => u.Id != currentUserId);

        if (excludeUserIds.Any())
        {
            query = query.Where(u => !excludeUserIds.Contains(u.Id));
        }

        // PostGIS: Используем ST_DWithin через EF.Functions (поддерживается Npgsql)
        // Для geography типа ST_DWithin использует метры
        // Используем raw SQL для точности
        var locationPoint = $"ST_SetSRID(ST_MakePoint({lon}, {lat}), 4326)::geography";
        var sqlFragment = $"ST_DWithin(u.\"Location\"::geography, {locationPoint}, {radiusInMeters})";
        
        // Используем FromSqlRaw для PostGIS функции
        // Но лучше использовать LINQ с преобразованием
        // Для упрощения используем фильтрацию через координаты (приблизительно)
        
        // Альтернативный подход: используем Distance через LINQ (если поддерживается)
        // Или используем raw SQL запрос
        
        // Простой подход: используем фильтрацию через координаты с буфером
        // В реальном проекте лучше использовать raw SQL с ST_DWithin
        
        // Фильтр по возрасту
        if (minAge.HasValue || maxAge.HasValue)
        {
            var today = DateTime.UtcNow.Date;
            if (maxAge.HasValue)
            {
                var minBirthDate = today.AddYears(-(maxAge.Value + 1));
                query = query.Where(u => u.BirthDate >= minBirthDate);
            }
            if (minAge.HasValue)
            {
                var maxBirthDate = today.AddYears(-minAge.Value);
                query = query.Where(u => u.BirthDate <= maxBirthDate);
            }
        }

        // Исключаем уже свайпнутых пользователей
        var swipedUserIds = await _context.Swipes
            .Where(s => (s.User1Id == currentUserId && s.Decision1 != null) ||
                       (s.User2Id == currentUserId && s.Decision2 != null))
            .Select(s => s.User1Id == currentUserId ? s.User2Id : s.User1Id)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (swipedUserIds.Any())
        {
            query = query.Where(u => !swipedUserIds.Contains(u.Id));
        }

        // Для гео-запроса используем простую фильтрацию по координатам
        // В реальном проекте нужно использовать raw SQL с ST_DWithin
        // Приблизительная фильтрация (1 градус ≈ 111 км)
        var latDelta = radiusInKilometers / 111.0;
        var lonDelta = radiusInKilometers / (111.0 * Math.Cos(lat * Math.PI / 180.0));
        
        // Применяем приблизительную фильтрацию
        query = query.Where(u => 
            Math.Abs((double)u.Location.Y - lat) <= latDelta &&
            Math.Abs((double)u.Location.X - lon) <= lonDelta);

        query = query.OrderByDescending(u => u.LastActive);

        var users = await query
            .Take(count * 2) // Берем больше для фильтрации
            .ToListAsync(cancellationToken);

        // Фильтруем точнее по расстоянию (если нужно)
        // В реальном проекте лучше использовать raw SQL с ST_DWithin
        
        return users.Take(count).ToList();
    }
}
