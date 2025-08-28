using API.Repositories.PgVectorDbContext.Configurations;
using API.Repositories.PgVectorDbContext.Entities;
using Microsoft.EntityFrameworkCore;

namespace API.Repositories.PgVectorDbContext;

public sealed class PgVectorDbContext : DbContext
{
    private readonly int _embeddingDimensions;


    public PgVectorDbContext(DbContextOptions<PgVectorDbContext> options, int embeddingDimensions = 1536)
        : base(options)
    {
        _embeddingDimensions = embeddingDimensions;
    }


    public DbSet<RagDocument> Documents => Set<RagDocument>();
    public DbSet<RagChunk> Chunks => Set<RagChunk>();


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
// Ensure migrations include CREATE EXTENSION vector;
        modelBuilder.HasPostgresExtension("vector");


        modelBuilder.ApplyConfiguration(new RagDocumentConfig());
        modelBuilder.ApplyConfiguration(new RagChunkConfig(_embeddingDimensions));


        base.OnModelCreating(modelBuilder);
    }
}