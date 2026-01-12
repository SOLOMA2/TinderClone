using TinderClone.Domain.Common;

namespace TinderClone.Domain.Entities;

public class UserPhoto : BaseEntity
{
    public Guid UserId { get; private set; }
    public string Url { get; private set; } = string.Empty;
    public bool IsMain { get; private set; }
    public DateTime UploadedAt { get; private set; } = DateTime.UtcNow;

    public virtual User User { get; private set; } = null!;

    private UserPhoto() { }

    internal UserPhoto(string url, bool isMain)
    {
        if (string.IsNullOrWhiteSpace(url)) throw new ArgumentException("URL фото не может быть пустым");
        Url = url;
        IsMain = isMain;
    }

    internal void SetMainStatus(bool isMain)
    {
        IsMain = isMain;
    }
}