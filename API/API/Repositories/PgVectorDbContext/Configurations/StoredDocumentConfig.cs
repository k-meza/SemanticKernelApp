using API.Repositories.PgVectorDbContext.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace API.Repositories.PgVectorDbContext.Configurations;

public sealed class StoredDocumentConfig : IEntityTypeConfiguration<StoredDocument>
{
    public void Configure(EntityTypeBuilder<StoredDocument> builder)
    {
        builder.ToTable("stored_documents");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.DocId)
            .IsRequired();

        builder.Property(x => x.FileName)
            .HasMaxLength(1024)
            .IsRequired();

        builder.Property(x => x.SizeBytes)
            .IsRequired();

        builder.Property(x => x.Content)
            .IsRequired(); // bytea in PostgreSQL

        builder.Property(x => x.CreatedAt)
            .HasColumnType("timestamptz")
            .IsRequired();

        // Optional FK to RagDocument if desired; uncomment if RagDocument is in the same schema
        builder.HasOne<RagDocument>()
            .WithMany()
            .HasForeignKey(x => x.DocId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.DocId);
    }
}