namespace Archiver.Application.Abstractions;

public interface IArchiveCodec
{
    ArchiveOperationResult Compress(ArchiveInput input, int maxCodeLength, string? password);

    ArchiveOperationResult Decompress(ArchiveInput input, string? password);
}
