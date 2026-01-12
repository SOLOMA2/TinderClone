using TinderClone.Domain.Entities;

namespace TinderClone.Domain.Interfaces;

public interface IMatchRepository
{
    void Add(Match match);

    Task<Match?> GetByIdWithHistoryAsync(Guid matchId, CancellationToken cancellationToken = default);

    Task<List<Match>> GetMatchesForUserAsync(Guid userId, CancellationToken cancellationToken = default);
}