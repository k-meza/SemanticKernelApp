using API.Repositories.PgVectorDbContext.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace API.Repositories.PgVectorDbContext.Configurations;

public sealed class RagChunkConfig : IEntityTypeConfiguration<RagChunk>
{
    private readonly int _dims;
    public RagChunkConfig(int dims) => _dims = dims;


    public void Configure(EntityTypeBuilder<RagChunk> b)
    {
        b.ToTable("rag_chunks");
        b.HasKey(x => x.ChunkId);


        b.Property(x => x.Content)
            .IsRequired();


// Vector column with explicit dimension
        b.Property(x => x.Embedding)
            .HasColumnType($"vector({_dims})")
            .IsRequired();


        b.HasIndex(x => new { x.DocId, x.ChunkIndex })
            .HasDatabaseName("rag_chunks_doc_idx");


        b.HasOne(x => x.Document)
            .WithMany(d => d.Chunks)
            .HasForeignKey(x => x.DocId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}