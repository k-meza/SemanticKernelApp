namespace API.Repositories.PgVectorDbContext.Entities;

public sealed class StoredDocument
{
    public Guid Id { get; set; }
    public Guid DocId { get; set; } // references RagDocument.DocId (no FK required, but recommended)

    public string FileName { get; set; } = default!;
    public long SizeBytes { get; set; }

    // Raw bytes of the uploaded file
    public byte[] Content { get; set; } = default!;

    public DateTimeOffset CreatedAt { get; set; }
}