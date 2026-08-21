using FluxoCaixa.Transactions.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FluxoCaixa.Transactions.Infrastructure.Mappings;

public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable("Transactions");

        builder.HasKey(t => t.Id);

        builder.ComplexProperty(t => t.Amount, amountBuilder =>
        {
            amountBuilder.Property(m => m.Value)
                .HasColumnName("Amount")
                .HasPrecision(18, 2)
                .IsRequired();

            amountBuilder.Property(m => m.Currency)
                .HasColumnName("Currency")
                .HasMaxLength(3)
                .IsRequired();
                });

        builder.Property(t => t.TransactionType)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(t => t.Description)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(t => t.CreatedAt)
            .IsRequired();
    }
}