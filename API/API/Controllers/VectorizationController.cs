using API.Domain.Vectorization.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/vectorization")]
public class VectorizationController : ControllerBase
{
    private readonly IVectorizationService _vectorization;

    public VectorizationController(IVectorizationService vectorization)
    {
        _vectorization = vectorization;
    }

    [HttpPost("ingest")]
    [RequestSizeLimit(50_000_000)] // adjust as needed   
    public async Task<IActionResult> Ingest(IFormFile file, CancellationToken ct)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file uploaded.");

        await using var stream = file.OpenReadStream();
        var result = await _vectorization.IngestAsync(stream, file.FileName, title: file.FileName, metadata: null, ct);
        return Ok(result);
    }

    [HttpPost("ingest-file")]
    public async Task<IActionResult> IngestFilePath([FromQuery] string path, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(path))
            return BadRequest("Path is required.");

        var result = await _vectorization.IngestFileAsync(path, null, null, ct);
        return Ok(result);
    }
}