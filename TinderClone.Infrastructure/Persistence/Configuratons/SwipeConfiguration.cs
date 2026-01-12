using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TinderClone.Domain.Entities;

namespace TinderClone.Infrastructure.Persistence.Configurations;

public class SwipeConfiguration : IEntityTypeConfiguration<Swipe>
{
    public void Configure(EntityTypeBuilder<Swipe> builder)
    {
        // Композитный ключ для предотвращения race condition
        builder.HasKey(s => new { s.User1Id, s.User2Id });

        // Индексы для быстрого поиска
        builder.HasIndex(s => s.User1Id);
        builder.HasIndex(s => s.User2Id);
        builder.HasIndex(s => new { s.User1Id, s.Decision1 });
        builder.HasIndex(s => new { s.User2Id, s.Decision2 });

        builder.Property(s => s.Decision1).IsRequired(false);
        builder.Property(s => s.Decision2).IsRequired(false);
        builder.Property(s => s.IsSuperLike1).HasDefaultValue(false);
        builder.Property(s => s.IsSuperLike2).HasDefaultValue(false);

        builder.HasOne(s => s.User1)
               .WithMany()
               .HasForeignKey(s => s.User1Id)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.User2)
               .WithMany()
               .HasForeignKey(s => s.User2Id)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
