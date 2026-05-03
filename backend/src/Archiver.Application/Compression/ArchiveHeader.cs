namespace Archiver.Application.Compression;

internal sealed record ArchiveHeader(
    ArchiveFlags Flags,
    ulong OriginalLength,
    ulong BitLength,
    int MaxCodeLength,
    string OriginalFileName,
    Dictionary<byte, int> CodeLengths,
    byte[]? Salt,
    byte[]? Nonce,
    byte[]? Tag);
