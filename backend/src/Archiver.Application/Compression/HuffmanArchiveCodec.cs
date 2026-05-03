using System.Text;
using Archiver.Application;
using Archiver.Application.Abstractions;

namespace Archiver.Application.Compression;

public sealed class HuffmanArchiveCodec : IArchiveCodec
{
    public ArchiveOperationResult Compress(ArchiveInput input, int maxCodeLength, string? password)
    {
        var frequencies = CountFrequencies(input.Content);
        var codeLengths = HuffmanLengthBuilder.Build(frequencies, maxCodeLength);
        var codes = CanonicalHuffman.BuildCodes(codeLengths);
        var writer = new BitWriter();

        foreach (var symbol in input.Content)
        {
            var code = codes[symbol];
            writer.Write(code.Bits, code.Length);
        }

        var payload = writer.ToArray();
        byte[]? salt = null;
        byte[]? nonce = null;
        byte[]? tag = null;
        var flags = ArchiveFlags.None;

        if (!string.IsNullOrEmpty(password))
        {
            var protectedPayload = PasswordProtector.Protect(payload, password);
            payload = protectedPayload.CipherText;
            salt = protectedPayload.Salt;
            nonce = protectedPayload.Nonce;
            tag = protectedPayload.Tag;
            flags |= ArchiveFlags.PasswordProtected;
        }

        var header = new ArchiveHeader(
            flags,
            (ulong)input.Content.LongLength,
            writer.BitLength,
            maxCodeLength,
            input.FileName,
            codeLengths,
            salt,
            nonce,
            tag);

        var archive = WriteArchive(header, payload);
        var outputName = input.FileName.EndsWith(ArchiveDefaults.Extension, StringComparison.OrdinalIgnoreCase)
            ? input.FileName
            : input.FileName + ArchiveDefaults.Extension;

        return new ArchiveOperationResult(
            archive,
            outputName,
            input.Content.LongLength,
            archive.LongLength,
            CalculateRatio(input.Content.LongLength, archive.LongLength),
            maxCodeLength,
            !string.IsNullOrEmpty(password));
    }

    public ArchiveOperationResult Decompress(ArchiveInput input, string? password)
    {
        var (header, payload) = ReadArchive(input.Content);

        if (header.Flags.HasFlag(ArchiveFlags.PasswordProtected))
        {
            payload = PasswordProtector.Unprotect(payload, password, header.Salt, header.Nonce, header.Tag);
        }

        var expectedPayloadLength = checked((long)((header.BitLength + 7UL) / 8UL));
        if (payload.LongLength != expectedPayloadLength)
        {
            throw new ArchiveValidationException("Archive payload length does not match the bit stream metadata.");
        }

        var result = Decode(payload, header);
        var fileName = string.IsNullOrWhiteSpace(header.OriginalFileName)
            ? Path.GetFileNameWithoutExtension(input.FileName)
            : header.OriginalFileName;

        return new ArchiveOperationResult(
            result,
            fileName,
            input.Content.LongLength,
            result.LongLength,
            CalculateRatio(input.Content.LongLength, result.LongLength),
            header.MaxCodeLength,
            header.Flags.HasFlag(ArchiveFlags.PasswordProtected));
    }

    private static Dictionary<byte, ulong> CountFrequencies(byte[] content)
    {
        var frequencies = new Dictionary<byte, ulong>();

        foreach (var symbol in content)
        {
            frequencies.TryGetValue(symbol, out var value);
            frequencies[symbol] = value + 1;
        }

        return frequencies;
    }

    private static byte[] Decode(byte[] payload, ArchiveHeader header)
    {
        if (header.OriginalLength > ArchiveDefaults.MaxFileSizeBytes)
        {
            throw new ArchiveValidationException("Archive declares a file larger than the 100 MB limit.");
        }

        var root = CanonicalHuffman.BuildDecodeTree(header.CodeLengths);
        var reader = new BitReader(payload, header.BitLength);
        using var output = new MemoryStream((int)header.OriginalLength);
        var node = root;

        while (reader.HasBits)
        {
            node = node.Next(reader.ReadBit());

            if (!node.Symbol.HasValue)
            {
                continue;
            }

            if ((ulong)output.Length >= header.OriginalLength)
            {
                throw new ArchiveValidationException("Archive contains more decoded data than declared.");
            }

            output.WriteByte(node.Symbol.Value);
            node = root;
        }

        if ((ulong)output.Length != header.OriginalLength)
        {
            throw new ArchiveValidationException("Archive ended before the declared amount of data was decoded.");
        }

        return output.ToArray();
    }

    private static byte[] WriteArchive(ArchiveHeader header, byte[] payload)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);
        var fileNameBytes = Encoding.UTF8.GetBytes(header.OriginalFileName);

        if (fileNameBytes.Length > ushort.MaxValue)
        {
            throw new ArchiveValidationException("File name is too long.");
        }

        writer.Write(ArchiveDefaults.Magic);
        writer.Write(ArchiveDefaults.Version);
        writer.Write((byte)header.Flags);
        writer.Write(header.OriginalLength);
        writer.Write(header.BitLength);
        writer.Write((byte)header.MaxCodeLength);
        writer.Write((ushort)fileNameBytes.Length);
        writer.Write(fileNameBytes);
        writer.Write((ushort)header.CodeLengths.Count);

        foreach (var pair in header.CodeLengths.OrderBy(pair => pair.Key))
        {
            writer.Write(pair.Key);
            writer.Write((byte)pair.Value);
        }

        if (header.Flags.HasFlag(ArchiveFlags.PasswordProtected))
        {
            writer.Write(header.Salt!);
            writer.Write(header.Nonce!);
            writer.Write(header.Tag!);
        }

        writer.Write(payload);
        writer.Flush();
        return stream.ToArray();
    }

    private static (ArchiveHeader Header, byte[] Payload) ReadArchive(byte[] archive)
    {
        using var stream = new MemoryStream(archive);
        using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);

        try
        {
            var magic = reader.ReadBytes(ArchiveDefaults.Magic.Length);
            if (!magic.SequenceEqual(ArchiveDefaults.Magic))
            {
                throw new ArchiveValidationException("Unsupported file format. Expected a .huff archive.");
            }

            var version = reader.ReadByte();
            if (version != ArchiveDefaults.Version)
            {
                throw new ArchiveValidationException($"Unsupported archive version {version}.");
            }

            var flags = (ArchiveFlags)reader.ReadByte();
            var originalLength = reader.ReadUInt64();
            var bitLength = reader.ReadUInt64();
            var maxCodeLength = reader.ReadByte();
            var fileNameLength = reader.ReadUInt16();
            var fileName = Encoding.UTF8.GetString(reader.ReadBytes(fileNameLength));
            var codeLengthCount = reader.ReadUInt16();
            var codeLengths = new Dictionary<byte, int>(codeLengthCount);

            if (codeLengthCount is 0 or > 256)
            {
                throw new ArchiveValidationException("Archive code table is invalid.");
            }

            for (var i = 0; i < codeLengthCount; i++)
            {
                var symbol = reader.ReadByte();
                var length = reader.ReadByte();

                if (length is < ArchiveDefaults.MinCodeLength or > ArchiveDefaults.MaxCodeLength)
                {
                    throw new ArchiveValidationException("Archive contains an invalid code length.");
                }

                if (!codeLengths.TryAdd(symbol, length))
                {
                    throw new ArchiveValidationException("Archive contains duplicate code table entries.");
                }
            }

            byte[]? salt = null;
            byte[]? nonce = null;
            byte[]? tag = null;

            if (flags.HasFlag(ArchiveFlags.PasswordProtected))
            {
                salt = reader.ReadBytes(PasswordProtector.SaltSize);
                nonce = reader.ReadBytes(PasswordProtector.NonceSize);
                tag = reader.ReadBytes(PasswordProtector.TagSize);
            }

            var payload = reader.ReadBytes((int)(stream.Length - stream.Position));
            var header = new ArchiveHeader(flags, originalLength, bitLength, maxCodeLength, fileName, codeLengths, salt, nonce, tag);
            return (header, payload);
        }
        catch (EndOfStreamException exception)
        {
            throw new ArchiveValidationException("Archive header is incomplete.", exception);
        }
    }

    private static double CalculateRatio(long originalSize, long resultSize)
    {
        return originalSize == 0 ? 0 : Math.Round((double)resultSize / originalSize, 4);
    }
}
