using System;
using TinderClone.Domain.Common;

namespace TinderClone.Domain.Entities;

public class Swipe : BaseEntity
{
    public Guid OriginUserId { get; set; }
    public virtual User OriginUser { get; set; } = null!;

    public Guid TargetUserId { get; set; }
    public virtual User TargetUser { get; set; } = null!;

    public bool IsLike { get; set; }
}