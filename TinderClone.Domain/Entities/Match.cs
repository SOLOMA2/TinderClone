using TinderClone.Domain.Common;

namespace TinderClone.Domain.Entities;

public class Match : BaseEntity
{
    private readonly List<ChatMessage> _messages = new();

    public Guid UserAId { get; private set; }
    public Guid UserBId { get; private set; }
    public DateTime MatchedAt { get; private set; } = DateTime.UtcNow;

    public bool IsActive { get; private set; } = true;

    public virtual User UserA { get; private set; } = null!;
    public virtual User UserB { get; private set; } = null!;

    public virtual IReadOnlyCollection<ChatMessage> Messages => _messages.AsReadOnly();

    private Match() { }

    public Match(Guid userAId, Guid userBId)
    {
        if (userAId == userBId) throw new ArgumentException("Нельзя создать матч с самим собой");

        UserAId = userAId;
        UserBId = userBId;
        IsActive = true;
    }


    public void Unmatch()
    {
        IsActive = false;
    }

    public void SendMessage(Guid senderId, string text)
    {
        if (!IsActive) throw new InvalidOperationException("Нельзя писать в разорванный матч");
        if (senderId != UserAId && senderId != UserBId) throw new ArgumentException("Пользователь не является участником этого матча");

        _messages.Add(new ChatMessage(senderId, text));
    }

    public ChatMessage? GetLastMessage()
    {
        return _messages.OrderByDescending(m => m.SentAt).FirstOrDefault();
    }
}