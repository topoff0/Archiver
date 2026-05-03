namespace Archiver.Application.Compression;

internal sealed class BitReader(byte[] data, ulong bitLength)
{
    private ulong _position;

    public bool HasBits => _position < bitLength;

    public int ReadBit()
    {
        if (!HasBits)
        {
            throw new InvalidDataException("Encoded data ended unexpectedly.");
        }

        var byteIndex = (int)(_position / 8);
        var offset = 7 - (int)(_position % 8);
        _position++;
        return (data[byteIndex] >> offset) & 1;
    }
}
