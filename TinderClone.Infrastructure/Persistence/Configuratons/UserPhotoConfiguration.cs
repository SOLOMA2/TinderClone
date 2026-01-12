using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TinderClone.Domain.Entities;

namespace TinderClone.Infrastructure.Persistence.Configurations;

public class UserPhotoConfiguration : IEntityTypeConfiguration<UserPhoto>
{
    public void Configure(EntityTypeBuilder<UserPhoto> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Url).HasMaxLength(1000).IsRequired();
        
        // Индексы для оптимизации
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => new { x.UserId, x.IsMain });
        builder.HasIndex(x => x.UploadedAt);
    }
}
