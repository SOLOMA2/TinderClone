using System;
using TinderClone.Domain.Common;

namespace TinderClone.Domain.Entities;

public class UserPhoto : BaseEntity
{
    public string Url { get; set; } = string.Empty;
    public bool IsMain { get; set; } 

    public Guid UserId { get; set; }
    public virtual User User { get; set; } = null!;
}