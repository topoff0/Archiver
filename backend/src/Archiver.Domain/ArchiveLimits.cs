namespace Archiver.Domain;

public static class ArchiveLimits
{
    public const long MaxFileSizeBytes = 100L * 1024L * 1024L;
    public const int MinCodeLength = 1;
    public const int MaxCodeLength = 32;
    public const int DefaultMaxCodeLength = 32;
}
