using Archiver.Domain;

namespace Archiver.Application;

public static class ArchiveDefaults
{
    public const long MaxFileSizeBytes = ArchiveLimits.MaxFileSizeBytes;
    public const int MinCodeLength = ArchiveLimits.MinCodeLength;
    public const int MaxCodeLength = ArchiveLimits.MaxCodeLength;
    public const int DefaultMaxCodeLength = ArchiveLimits.DefaultMaxCodeLength;
    public const string Extension = ArchiveFormat.Extension;
    public const byte Version = ArchiveFormat.Version;

    public static byte[] Magic => ArchiveFormat.Magic;
}
