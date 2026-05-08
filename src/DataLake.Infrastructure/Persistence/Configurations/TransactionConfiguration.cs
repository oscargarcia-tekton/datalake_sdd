using DataLake.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataLake.Infrastructure.Persistence.Configurations;

internal sealed class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.HasKey(t => t.TransactionId);

        builder.Property(t => t.SourceFileName).HasMaxLength(500).IsRequired();
        builder.Property(t => t.NodeId).HasMaxLength(100).IsRequired();
        builder.Property(t => t.Currency).HasMaxLength(10).IsRequired();
        builder.Property(t => t.ClassificationMetadata).HasColumnType("nvarchar(max)");

        builder.HasIndex(t => t.SourceFileName);

        // Idempotency index on the computed processing key
        builder.HasIndex(nameof(Transaction.SourceFileName), nameof(Transaction.RecordSequence))
               .IsUnique()
               .HasDatabaseName("IX_Transactions_ProcessingId");
    }
}
