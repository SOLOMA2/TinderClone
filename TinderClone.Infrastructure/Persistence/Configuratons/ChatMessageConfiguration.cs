using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TinderClone.Domain.Entities;

namespace TinderClone.Infrastructure.Persistence.Configurations;

public class ChatMessageConfiguration : IEntityTypeConfiguration<ChatMessage>
{
    public void Configure(EntityTypeBuilder<ChatMessage> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Text).HasMaxLength(4096).IsRequired();
        
        // Индексы для оптимизации запросов
        builder.HasIndex(x => x.MatchId);
        builder.HasIndex(x => new { x.MatchId, x.SentAt });
        builder.HasIndex(x => x.SenderId);
    }
}
