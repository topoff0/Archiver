namespace Archiver.Application.Compression;

internal sealed record ProtectedPayload(byte[] CipherText, byte[] Salt, byte[] Nonce, byte[] Tag);
