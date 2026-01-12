using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TinderClone.Domain.Entities;

namespace TinderClone.Infrastructure.Persistence.Configurations;

public class MatchConfiguration : IEntityTypeConfiguration<Match>
{
    public void Configure(EntityTypeBuilder<Match> builder)
    {
        builder.HasKey(x => x.Id);

        // Композитный уникальный индекс для предотвращения дубликатов
        builder.HasIndex(x => new { x.UserAId, x.UserBId }).IsUnique();

        // Индексы для оптимизации запросов
        builder.HasIndex(x => x.UserAId);
        builder.HasIndex(x => x.UserBId);
        builder.HasIndex(x => new { x.UserAId, x.IsActive });
        builder.HasIndex(x => new { x.UserBId, x.IsActive });
        builder.HasIndex(x => x.MatchedAt);

        builder.HasOne(m => m.UserA)
               .WithMany()
               .HasForeignKey(m => m.UserAId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.UserB)
               .WithMany()
               .HasForeignKey(m => m.UserBId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(m => m.Messages)
               .WithOne(c => c.Match)
               .HasForeignKey(c => c.MatchId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata.FindNavigation(nameof(Match.Messages))!
               .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
