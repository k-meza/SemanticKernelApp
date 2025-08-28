using API.Repositories.PgVectorDbContext.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace API.Repositories.PgVectorDbContext.Configurations;

public sealed class RagDocumentConfig : IEntityTypeConfiguration<RagDocument>
{
    public void Configure(EntityTypeBuilder<RagDocument> b)
    {
        b.ToTable("rag_documents");
        b.HasKey(x => x.DocId);


        b.Property(x => x.Title)
            .HasMaxLength(512)
            .IsRequired();


        b.Property(x => x.SourcePath)
            .HasMaxLength(1024)
            .IsRequired();


        b.Property(x => x.Metadata)
            .HasColumnType("jsonb");


        b.Property(x => x.CreatedAt)
            .HasDefaultValueSql("now()");
    }
}