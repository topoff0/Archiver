namespace Archiver.Domain;

public static class ArchiveFormat
{
    public const string Extension = ".huff";
    public const byte Version = 1;
    public static readonly byte[] Magic = "HUFF"u8.ToArray();
}
