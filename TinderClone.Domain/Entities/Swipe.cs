namespace TinderClone.Domain.Entities;

/// <summary>
/// Структура для избежания race condition при параллельных лайках
/// User1_Id всегда меньше User2_Id (каноническая форма)
/// Decision1 - решение пользователя User1_Id
/// Decision2 - решение пользователя User2_Id
/// Композитный ключ (User1_Id, User2_Id) обеспечивает атомарность операции
/// </summary>
public class Swipe
{
    public Guid User1Id { get; private set; }
    public Guid User2Id { get; private set; }
    
    public bool? Decision1 { get; private set; } // Решение пользователя User1Id (null, false=дизлайк, true=лайк)
    public bool? Decision2 { get; private set; } // Решение пользователя User2Id (null, false=дизлайк, true=лайк)
    
    public bool IsSuperLike1 { get; private set; }
    public bool IsSuperLike2 { get; private set; }
    
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; private set; }

    public virtual User User1 { get; private set; } = null!;
    public virtual User User2 { get; private set; } = null!;

    private Swipe() { }

    /// <summary>
    /// Создает или обновляет свайп. Всегда сохраняет пользователей в каноническом порядке (меньший ID первый)
    /// </summary>
    public static (Guid user1Id, Guid user2Id) NormalizeIds(Guid userId1, Guid userId2)
    {
        if (userId1 == userId2)
            throw new ArgumentException("Нельзя свайпать самого себя");

        return userId1.CompareTo(userId2) < 0 
            ? (userId1, userId2) 
            : (userId2, userId1);
    }

    /// <summary>
    /// Создает новый свайп или обновляет существующий
    /// </summary>
    public static Swipe CreateOrUpdate(Guid fromUserId, Guid toUserId, bool isLike, bool isSuperLike = false)
    {
        var (user1Id, user2Id) = NormalizeIds(fromUserId, toUserId);
        var isFromUser1 = fromUserId == user1Id;

        return new Swipe
        {
            User1Id = user1Id,
            User2Id = user2Id,
            Decision1 = isFromUser1 ? isLike : null,
            Decision2 = !isFromUser1 ? isLike : null,
            IsSuperLike1 = isFromUser1 && isSuperLike,
            IsSuperLike2 = !isFromUser1 && isSuperLike,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Обновляет решение пользователя
    /// </summary>
    public void UpdateDecision(Guid userId, bool isLike, bool isSuperLike = false)
    {
        if (userId == User1Id)
        {
            Decision1 = isLike;
            IsSuperLike1 = isLike && isSuperLike;
        }
        else if (userId == User2Id)
        {
            Decision2 = isLike;
            IsSuperLike2 = isLike && isSuperLike;
        }
        else
        {
            throw new ArgumentException("Пользователь не участвует в этом свайпе");
        }

        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Проверяет, является ли это матчем (оба пользователя лайкнули друг друга)
    /// </summary>
    public bool IsMatch()
    {
        return Decision1 == true && Decision2 == true;
    }

    /// <summary>
    /// Получает решение конкретного пользователя
    /// </summary>
    public bool? GetDecision(Guid userId)
    {
        if (userId == User1Id) return Decision1;
        if (userId == User2Id) return Decision2;
        return null;
    }
}
