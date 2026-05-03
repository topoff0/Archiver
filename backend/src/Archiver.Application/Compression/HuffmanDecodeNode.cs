namespace Archiver.Application.Compression;

internal sealed class HuffmanDecodeNode
{
    public byte? Symbol { get; set; }
    public HuffmanDecodeNode? Left { get; private set; }
    public HuffmanDecodeNode? Right { get; private set; }

    public HuffmanDecodeNode GetOrCreateLeft()
    {
        return Left ??= new HuffmanDecodeNode();
    }

    public HuffmanDecodeNode GetOrCreateRight()
    {
        return Right ??= new HuffmanDecodeNode();
    }

    public HuffmanDecodeNode Next(int bit)
    {
        var next = bit == 0 ? Left : Right;
        return next ?? throw new InvalidDataException("Encoded data does not match the archive code table.");
    }
}
