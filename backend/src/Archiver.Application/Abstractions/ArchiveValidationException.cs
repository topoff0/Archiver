namespace Archiver.Application.Abstractions;

public sealed class ArchiveValidationException : Exception
{
    public ArchiveValidationException(string message)
        : base(message)
    {
    }

    public ArchiveValidationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
