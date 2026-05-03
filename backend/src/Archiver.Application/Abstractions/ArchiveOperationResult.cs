namespace Archiver.Application.Abstractions;

public sealed record ArchiveOperationResult(
    byte[] Content,
    string FileName,
    long OriginalSize,
    long ResultSize,
    double CompressionRatio,
    int MaxCodeLength,
    bool PasswordProtected);
