using System.Security.Cryptography;
using System.Text;
using Archiver.Application.Abstractions;

namespace Archiver.Application.Compression;

internal static class PasswordProtector
{
    public const int SaltSize = 16;
    public const int NonceSize = 12;
    public const int TagSize = 16;
    private const int KeySize = 32;
    private const int Iterations = 150_000;

    public static ProtectedPayload Protect(byte[] payload, string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var tag = new byte[TagSize];
        var cipherText = new byte[payload.Length];
        var key = DeriveKey(password, salt);

        using var aes = new AesGcm(key, TagSize);
        aes.Encrypt(nonce, payload, cipherText, tag);

        return new ProtectedPayload(cipherText, salt, nonce, tag);
    }

    public static byte[] Unprotect(byte[] cipherText, string? password, byte[]? salt, byte[]? nonce, byte[]? tag)
    {
        if (string.IsNullOrEmpty(password))
        {
            throw new ArchiveValidationException("Password is required to decompress this archive.");
        }

        if (salt?.Length != SaltSize || nonce?.Length != NonceSize || tag?.Length != TagSize)
        {
            throw new ArchiveValidationException("Archive encryption metadata is corrupted.");
        }

        var payload = new byte[cipherText.Length];
        var key = DeriveKey(password, salt);

        try
        {
            using var aes = new AesGcm(key, TagSize);
            aes.Decrypt(nonce, cipherText, tag, payload);
            return payload;
        }
        catch (CryptographicException exception)
        {
            throw new ArchiveValidationException("Invalid password or corrupted archive.", exception);
        }
    }

    private static byte[] DeriveKey(string password, byte[] salt)
    {
        return Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            salt,
            Iterations,
            HashAlgorithmName.SHA256,
            KeySize);
    }
}
