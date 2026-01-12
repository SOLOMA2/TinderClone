using Microsoft.EntityFrameworkCore;
using TinderClone.Domain.Entities;
using TinderClone.Domain.Interfaces;

namespace TinderClone.Infrastructure.Persistence.Repositories;

public class MatchRepository : IMatchRepository
{
    private readonly AppDbContext _context;

    public MatchRepository(AppDbContext context)
    {
        _context = context;
    }

    public void Add(Match match)
    {
        _context.Matches.Add(match);
    }

    public async Task<Match?> GetByIdWithHistoryAsync(Guid matchId, CancellationToken cancellationToken = default)
    {
        return await _context.Matches
            // Подгружаем собеседников
            .Include(m => m.UserA)
            .Include(m => m.UserB)
            // Подгружаем сообщения (сортировку сделаем в памяти или через OrderBy в Include, если БД позволяет)
            .Include(m => m.Messages)
            .FirstOrDefaultAsync(m => m.Id == matchId, cancellationToken);
    }

    public async Task<List<Match>> GetMatchesForUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        // Сложный запрос:
        // Дай мне матчи, где Я являюсь либо UserA, либо UserB.
        // И подгрузи собеседников, чтобы я мог отобразить их имена.

        return await _context.Matches
            .AsNoTracking()
            .Include(m => m.UserA)
                .ThenInclude(u => u.Photos) // Нужна аватарка собеседника
            .Include(m => m.UserB)
                .ThenInclude(u => u.Photos)
            // Фильтр: или я А, или я Б
            .Where(m => m.UserAId == userId || m.UserBId == userId)
            // Сортировка по свежести (самые новые сверху)
            .OrderByDescending(m => m.MatchedAt)
            .ToListAsync(cancellationToken);
    }
}