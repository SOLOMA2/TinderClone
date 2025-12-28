using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TinderClone.Domain.Common;
using TinderClone.Domain.Enums;

namespace TinderClone.Domain.Entities;

public class User : BaseEntity
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Bio { get; set; } = string.Empty;

    public DateTime BirthDate { get; set; }

    public int Age
    {
        get
        {
            var today = DateTime.Today;
            var age = today.Year - BirthDate.Year;
            if (BirthDate.Date > today.AddYears(-age)) age--;
            return age;
        }
    }

    public Gender Gender { get; set; }
    public Gender PreferredGender { get; set; }

    public double Latitude { get; set; }
    public double Longitude { get; set; }

    public DateTime LastActive { get; set; } = DateTime.UtcNow;

    public virtual ICollection<UserPhoto> Photos { get; set; } = new List<UserPhoto>();

    public virtual ICollection<Swipe> SwipesSent { get; set; } = new List<Swipe>();
    public virtual ICollection<Swipe> SwipesReceived { get; set; } = new List<Swipe>();
    public virtual ICollection<Match> MatchesAsUserA { get; set; } = new List<Match>();
    public virtual ICollection<Match> MatchesAsUserB { get; set; } = new List<Match>();
}