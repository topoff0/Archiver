using System.Globalization;
using Archiver.Application;
using Archiver.Application.Abstractions;
using Archiver.Application.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Archiver.Api.Controllers;

[ApiController]
[Route("api/archive")]
public sealed class ArchiveController(ArchiveService archiveService) : ControllerBase
{
    [HttpPost("compress")]
    [RequestSizeLimit(ArchiveDefaults.MaxFileSizeBytes + 1024 * 1024)]
    public async Task<IActionResult> Compress([FromForm] ArchiveRequest request, CancellationToken cancellationToken)
    {
        return await ProcessAsync(
            request,
            input => archiveService.Compress(input, request.MaxCodeLength, request.Password),
            cancellationToken);
    }

    [HttpPost("decompress")]
    [RequestSizeLimit(ArchiveDefaults.MaxFileSizeBytes + 1024 * 1024)]
    public async Task<IActionResult> Decompress([FromForm] ArchiveRequest request, CancellationToken cancellationToken)
    {
        return await ProcessAsync(
            request,
            input => archiveService.Decompress(input, request.Password),
            cancellationToken);
    }

    private async Task<IActionResult> ProcessAsync(
        ArchiveRequest request,
        Func<ArchiveInput, ArchiveOperationResult> operation,
        CancellationToken cancellationToken)
    {
        try
        {
            var input = await ReadInputAsync(request.File, cancellationToken);
            var result = operation(input);
            AddResultHeaders(result);

            return File(result.Content, "application/octet-stream", result.FileName);
        }
        catch (ArchiveValidationException exception)
        {
            return BadRequest(new ErrorResponse(exception.Message));
        }
        catch (InvalidDataException exception)
        {
            return BadRequest(new ErrorResponse(exception.Message));
        }
    }

    private static async Task<ArchiveInput> ReadInputAsync(IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null)
        {
            throw new ArchiveValidationException("File is required.");
        }

        if (file.Length > ArchiveDefaults.MaxFileSizeBytes)
        {
            throw new ArchiveValidationException("File size must not exceed 100 MB.");
        }

        await using var stream = file.OpenReadStream();
        using var memory = new MemoryStream((int)Math.Min(file.Length, int.MaxValue));
        await stream.CopyToAsync(memory, cancellationToken);

        return new ArchiveInput(memory.ToArray(), Path.GetFileName(file.FileName));
    }

    private void AddResultHeaders(ArchiveOperationResult result)
    {
        Response.Headers["X-Original-Size"] = result.OriginalSize.ToString();
        Response.Headers["X-Result-Size"] = result.ResultSize.ToString();
        Response.Headers["X-Compression-Ratio"] = result.CompressionRatio.ToString("0.####", CultureInfo.InvariantCulture);
        Response.Headers["X-Max-Code-Length"] = result.MaxCodeLength.ToString();
        Response.Headers["X-Password-Protected"] = result.PasswordProtected.ToString();
        Response.Headers["X-File-Name"] = Uri.EscapeDataString(result.FileName);
    }

    public sealed class ArchiveRequest
    {
        public IFormFile? File { get; init; }
        public int? MaxCodeLength { get; init; }
        public string? Password { get; init; }
    }

    public sealed record ErrorResponse(string Message);
}
