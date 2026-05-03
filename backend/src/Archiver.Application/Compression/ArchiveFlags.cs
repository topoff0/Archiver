namespace Archiver.Application.Compression;

[Flags]
internal enum ArchiveFlags : byte
{
    None = 0,
    PasswordProtected = 1
}
