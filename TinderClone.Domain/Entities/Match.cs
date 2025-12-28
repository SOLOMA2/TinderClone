using System;
using TinderClone.Domain.Common;

namespace TinderClone.Domain.Entities;

public class Match : BaseEntity
{
    public Guid UserAId { get; set; }
    public virtual User UserA { get; set; } = null!;

    public Guid UserBId { get; set; }
    public virtual User UserB { get; set; } = null!;
}