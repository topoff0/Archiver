namespace Archiver.Application.Abstractions;

public sealed record ArchiveInput(byte[] Content, string FileName);
