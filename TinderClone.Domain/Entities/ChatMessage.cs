using TinderClone.Domain.Common;

namespace TinderClone.Domain.Entities;

public class ChatMessage : BaseEntity
{
    public Guid MatchId { get; private set; } 
    public Guid SenderId { get; private set; }
    public string Text { get; private set; } = string.Empty;
    public DateTime SentAt { get; private set; } = DateTime.UtcNow;
    public bool IsRead { get; private set; } = false;
    public DateTime? ReadAt { get; private set; }

    public virtual Match Match { get; private set; } = null!;

    private ChatMessage() { }

    internal ChatMessage(Guid senderId, string text)
    {
        if (string.IsNullOrWhiteSpace(text)) throw new ArgumentException("Сообщение не может быть пустым");
        if (text.Length > 4096) throw new ArgumentException("Сообщение слишком длинное");

        SenderId = senderId;
        Text = text.Trim();
    }

    public void MarkAsRead()
    {
        if (!IsRead)
        {
            IsRead = true;
            ReadAt = DateTime.UtcNow;
        }
    }
}