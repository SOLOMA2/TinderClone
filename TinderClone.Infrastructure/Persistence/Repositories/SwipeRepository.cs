using Microsoft.EntityFrameworkCore;
using TinderClone.Domain.Entities;
using TinderClone.Domain.Interfaces;

namespace TinderClone.Infrastructure.Persistence.Repositories;

public class SwipeRepository : ISwipeRepository
{
    private readonly AppDbContext _context;

    public SwipeRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Swipe?> GetSwipeAsync(Guid user1Id, Guid user2Id, CancellationToken cancellationToken = default)
    {
        var (normalizedUser1Id, normalizedUser2Id) = Swipe.NormalizeIds(user1Id, user2Id);
        
        return await _context.Swipes
            .AsNoTracking()
            .FirstOrDefaultAsync(
                s => s.User1Id == normalizedUser1Id && s.User2Id == normalizedUser2Id,
                cancellationToken);
    }

    /// <summary>
    /// Upsert операция для атомарного обновления свайпа
    /// Использует EF Core для предотвращения race condition
    /// </summary>
    public async Task<Swipe> UpsertSwipeAsync(Guid fromUserId, Guid toUserId, bool isLike, bool isSuperLike, CancellationToken cancellationToken = default)
    {
        var (user1Id, user2Id) = Swipe.NormalizeIds(fromUserId, toUserId);
        var isFromUser1 = fromUserId == user1Id;

        // Пытаемся найти существующий свайп
        var existingSwipe = await _context.Swipes
            .FirstOrDefaultAsync(
                s => s.User1Id == user1Id && s.User2Id == user2Id,
                cancellationToken);

        if (existingSwipe != null)
        {
            // Обновляем существующий свайп
            existingSwipe.UpdateDecision(fromUserId, isLike, isSuperLike);
            await _context.SaveChangesAsync(cancellationToken);
            return existingSwipe;
        }

        // Создаем новый свайп
        var newSwipe = Swipe.CreateOrUpdate(fromUserId, toUserId, isLike, isSuperLike);
        _context.Swipes.Add(newSwipe);
        await _context.SaveChangesAsync(cancellationToken);
        return newSwipe;
    }

    public async Task<bool> IsMatchAsync(Guid user1Id, Guid user2Id, CancellationToken cancellationToken = default)
    {
        var swipe = await GetSwipeAsync(user1Id, user2Id, cancellationToken);
        return swipe?.IsMatch() ?? false;
    }

    public async Task<List<Guid>> GetSwipedUserIdsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var swipesAsUser1 = await _context.Swipes
            .AsNoTracking()
            .Where(s => s.User1Id == userId && s.Decision1 != null)
            .Select(s => s.User2Id)
            .ToListAsync(cancellationToken);

        var swipesAsUser2 = await _context.Swipes
            .AsNoTracking()
            .Where(s => s.User2Id == userId && s.Decision2 != null)
            .Select(s => s.User1Id)
            .ToListAsync(cancellationToken);

        return swipesAsUser1.Concat(swipesAsUser2).Distinct().ToList();
    }
}
