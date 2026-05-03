namespace Archiver.Application.Compression;

internal static class CanonicalHuffman
{
    public static Dictionary<byte, HuffmanCode> BuildCodes(IReadOnlyDictionary<byte, int> lengths)
    {
        var result = new Dictionary<byte, HuffmanCode>(lengths.Count);
        var code = 0UL;
        var previousLength = 0;

        foreach (var pair in lengths.OrderBy(pair => pair.Value).ThenBy(pair => pair.Key))
        {
            code <<= pair.Value - previousLength;
            result[pair.Key] = new HuffmanCode(code, pair.Value);
            code++;
            previousLength = pair.Value;
        }

        return result;
    }

    public static HuffmanDecodeNode BuildDecodeTree(IReadOnlyDictionary<byte, int> lengths)
    {
        var root = new HuffmanDecodeNode();

        foreach (var pair in BuildCodes(lengths))
        {
            var node = root;
            var code = pair.Value;

            for (var i = code.Length - 1; i >= 0; i--)
            {
                var bit = (code.Bits >> i) & 1UL;
                node = bit == 0 ? node.GetOrCreateLeft() : node.GetOrCreateRight();
            }

            node.Symbol = pair.Key;
        }

        return root;
    }
}
