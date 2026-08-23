using FluxoCaixa.Transactions.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FluxoCaixa.Transactions.Infrastructure.Outbox;

public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.Type)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(o => o.Content)
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(o => o.OccurredOnUtc)
            .IsRequired();

        builder.Property(o => o.ProcessedOnUtc)
            .IsRequired(false);
            
        builder.HasIndex(o => o.ProcessedOnUtc);

        builder.Property(o => o.Error)
            .IsRequired(false);

    }
}
