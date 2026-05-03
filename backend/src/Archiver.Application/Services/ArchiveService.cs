using Archiver.Application.Abstractions;
using Archiver.Domain;

namespace Archiver.Application.Services;

public sealed class ArchiveService(IArchiveCodec codec)
{
    public ArchiveOperationResult Compress(ArchiveInput input, int? maxCodeLength, string? password)
    {
        ValidateInput(input);

        var codeLength = maxCodeLength ?? ArchiveLimits.DefaultMaxCodeLength;
        if (codeLength is < ArchiveLimits.MinCodeLength or > ArchiveLimits.MaxCodeLength)
        {
            throw new ArchiveValidationException(
                $"Maximum code length must be between {ArchiveLimits.MinCodeLength} and {ArchiveLimits.MaxCodeLength} bits.");
        }

        return codec.Compress(input, codeLength, NormalizePassword(password));
    }

    public ArchiveOperationResult Decompress(ArchiveInput input, string? password)
    {
        ValidateInput(input);
        return codec.Decompress(input, NormalizePassword(password));
    }

    private static void ValidateInput(ArchiveInput input)
    {
        if (input.Content.Length == 0)
        {
            throw new ArchiveValidationException("File is empty.");
        }

        if (input.Content.Length > ArchiveLimits.MaxFileSizeBytes)
        {
            throw new ArchiveValidationException("File size must not exceed 100 MB.");
        }
    }

    private static string? NormalizePassword(string? password)
    {
        return string.IsNullOrWhiteSpace(password) ? null : password;
    }
}
