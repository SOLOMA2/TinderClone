using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NetTopologySuite.IO;
using TinderClone.Domain.Entities;

namespace TinderClone.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.LastName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Bio).HasMaxLength(1000);

        // PostGIS: Используем NetTopologySuite для геолокации
        builder.Property(u => u.Location)
               .HasColumnType("geography (point)")
               .IsRequired();

        // Пространственный индекс GIST для быстрых гео-запросов
        builder.HasIndex(u => u.Location)
               .HasMethod("GIST");

        builder.HasIndex(x => x.Gender);
        builder.HasIndex(x => x.PreferredGender);
        builder.HasIndex(x => x.LastActive);
        builder.HasIndex(x => new { x.Gender, x.PreferredGender, x.LastActive });

        builder.HasMany(u => u.Photos)
               .WithOne(p => p.User)
               .HasForeignKey(p => p.UserId)
               .OnDelete(DeleteBehavior.Cascade); 

        builder.Metadata.FindNavigation(nameof(User.Photos))!
               .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(u => u.Matches);
        builder.Ignore(u => u.Latitude);
        builder.Ignore(u => u.Longitude);
    }
}
