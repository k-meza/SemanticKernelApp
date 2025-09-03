using API.Repositories.PgVectorDbContext.Configurations;
using API.Repositories.PgVectorDbContext.Entities;
using Microsoft.EntityFrameworkCore;

namespace API.Repositories.PgVectorDbContext;

public sealed class PgVectorDbContext : DbContext
{
    private readonly int _embeddingDimensions = 1536; // default

    public PgVectorDbContext(DbContextOptions<PgVectorDbContext> options) : base(options)
    {
        
    }
    
    public PgVectorDbContext(DbContextOptions<PgVectorDbContext> options, int embeddingDimensions) : base(options)
    {
        _embeddingDimensions = embeddingDimensions;
    }


    public DbSet<RagDocument> Documents => Set<RagDocument>();
    public DbSet<RagChunk> Chunks => Set<RagChunk>();
    public DbSet<StoredDocument> StoredDocuments => Set<StoredDocument>();

    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Ensure migrations include CREATE EXTENSION vector;
        modelBuilder.HasPostgresExtension("vector");

        modelBuilder.ApplyConfiguration(new RagDocumentConfig());
        modelBuilder.ApplyConfiguration(new RagChunkConfig(_embeddingDimensions));
        modelBuilder.ApplyConfiguration(new StoredDocumentConfig());

        base.OnModelCreating(modelBuilder);
    }
    
    
}