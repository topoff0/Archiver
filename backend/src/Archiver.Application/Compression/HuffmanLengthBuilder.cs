using Archiver.Application.Abstractions;

namespace Archiver.Application.Compression;

internal static class HuffmanLengthBuilder
{
    public static Dictionary<byte, int> Build(IReadOnlyDictionary<byte, ulong> frequencies, int maxCodeLength)
    {
        if (frequencies.Count == 1)
        {
            var onlySymbol = frequencies.Keys.First();
            return new Dictionary<byte, int> { [onlySymbol] = 1 };
        }

        var minRequiredLength = CeilingLog2(frequencies.Count);
        if (maxCodeLength < minRequiredLength)
        {
            throw new ArchiveValidationException(
                $"Maximum code length {maxCodeLength} is too small for {frequencies.Count} unique bytes. Minimum is {minRequiredLength}.");
        }

        var plainLengths = BuildPlainHuffmanLengths(frequencies);
        if (plainLengths.Values.Max() <= maxCodeLength)
        {
            return plainLengths;
        }

        return BuildLengthLimitedLengths(frequencies, maxCodeLength);
    }

    private static Dictionary<byte, int> BuildPlainHuffmanLengths(IReadOnlyDictionary<byte, ulong> frequencies)
    {
        var queue = new PriorityQueue<HuffmanNode, NodePriority>();
        var order = 0;

        foreach (var pair in frequencies.OrderBy(pair => pair.Value).ThenBy(pair => pair.Key))
        {
            queue.Enqueue(new HuffmanNode(pair.Key, pair.Value), new NodePriority(pair.Value, order));
            order++;
        }

        while (queue.Count > 1)
        {
            var left = queue.Dequeue();
            var right = queue.Dequeue();
            var parent = new HuffmanNode(left, right);
            queue.Enqueue(parent, new NodePriority(parent.Frequency, order));
            order++;
        }

        var lengths = new Dictionary<byte, int>();
        FillLengths(queue.Dequeue(), 0, lengths);
        return lengths;
    }

    private static void FillLengths(HuffmanNode node, int depth, Dictionary<byte, int> lengths)
    {
        if (node.IsLeaf)
        {
            lengths[node.Symbol!.Value] = Math.Max(1, depth);
            return;
        }

        FillLengths(node.Left!, depth + 1, lengths);
        FillLengths(node.Right!, depth + 1, lengths);
    }

    private static Dictionary<byte, int> BuildLengthLimitedLengths(IReadOnlyDictionary<byte, ulong> frequencies, int maxCodeLength)
    {
        var symbols = frequencies
            .OrderByDescending(pair => pair.Value)
            .ThenBy(pair => pair.Key)
            .Select(pair => new WeightedSymbol(pair.Key, pair.Value))
            .ToArray();

        var counts = FindBestLengthCounts(symbols, maxCodeLength);
        var result = new Dictionary<byte, int>();
        var symbolIndex = 0;

        for (var length = 1; length < counts.Length; length++)
        {
            for (var i = 0; i < counts[length]; i++)
            {
                result[symbols[symbolIndex].Symbol] = length;
                symbolIndex++;
            }
        }

        return result;
    }

    private static int[] FindBestLengthCounts(WeightedSymbol[] symbols, int maxCodeLength)
    {
        var symbolCount = symbols.Length;
        var prefixWeights = new double[symbolCount + 1];

        for (var i = 0; i < symbolCount; i++)
        {
            prefixWeights[i + 1] = prefixWeights[i] + symbols[i].Frequency;
        }

        var current = new Dictionary<StateKey, double>();
        var parents = new Dictionary<StateKey, Parent>[maxCodeLength + 1];
        current[new StateKey(0, 2)] = 0;

        for (var depth = 1; depth <= maxCodeLength; depth++)
        {
            var next = new Dictionary<StateKey, double>();
            parents[depth] = new Dictionary<StateKey, Parent>();

            foreach (var entry in current)
            {
                var state = entry.Key;
                var cost = entry.Value;

                if (state.Used == symbolCount && state.Slots == 0)
                {
                    AddState(next, parents[depth], state, cost, state, 0);
                    continue;
                }

                var maxLeavesHere = Math.Min(state.Slots, symbolCount - state.Used);
                for (var leavesHere = 0; leavesHere <= maxLeavesHere; leavesHere++)
                {
                    TryAddTransition(
                        next,
                        parents[depth],
                        state,
                        cost,
                        leavesHere,
                        depth,
                        maxCodeLength,
                        symbolCount,
                        prefixWeights);
                }
            }

            current = next;
        }

        var finalState = new StateKey(symbolCount, 0);
        if (!current.ContainsKey(finalState))
        {
            throw new ArchiveValidationException("Cannot build a prefix code with the selected maximum code length.");
        }

        var counts = new int[maxCodeLength + 1];
        var key = finalState;

        for (var depth = maxCodeLength; depth >= 1; depth--)
        {
            var parent = parents[depth][key];
            counts[depth] = parent.LeavesHere;
            key = parent.Previous;
        }

        return counts;
    }

    private static void TryAddTransition(
        Dictionary<StateKey, double> next,
        Dictionary<StateKey, Parent> parents,
        StateKey state,
        double cost,
        int leavesHere,
        int depth,
        int maxCodeLength,
        int symbolCount,
        double[] prefixWeights)
    {
        var used = state.Used + leavesHere;
        var internalNodes = state.Slots - leavesHere;
        var nextSlots = depth == maxCodeLength ? 0 : internalNodes * 2;

        if (depth == maxCodeLength && (internalNodes != 0 || used != symbolCount))
        {
            return;
        }

        var remaining = symbolCount - used;
        if (remaining == 0 && nextSlots != 0)
        {
            return;
        }

        if (remaining > 0)
        {
            if (nextSlots == 0 || nextSlots > remaining)
            {
                return;
            }

            var remainingDepth = maxCodeLength - depth;
            var maxCapacity = nextSlots * Pow2Capped(remainingDepth, symbolCount);
            if (remaining > maxCapacity)
            {
                return;
            }
        }

        var addedCost = (prefixWeights[used] - prefixWeights[state.Used]) * depth;
        var nextState = new StateKey(used, nextSlots);
        AddState(next, parents, nextState, cost + addedCost, state, leavesHere);
    }

    private static void AddState(
        Dictionary<StateKey, double> states,
        Dictionary<StateKey, Parent> parents,
        StateKey key,
        double cost,
        StateKey previous,
        int leavesHere)
    {
        if (states.TryGetValue(key, out var currentCost) && currentCost <= cost)
        {
            return;
        }

        states[key] = cost;
        parents[key] = new Parent(previous, leavesHere);
    }

    private static int CeilingLog2(int value)
    {
        var length = 0;
        var capacity = 1;

        while (capacity < value)
        {
            capacity <<= 1;
            length++;
        }

        return Math.Max(1, length);
    }

    private static int Pow2Capped(int exponent, int cap)
    {
        var value = 1;

        for (var i = 0; i < exponent; i++)
        {
            if (value >= cap)
            {
                return cap;
            }

            value <<= 1;
        }

        return Math.Min(value, cap);
    }

    private readonly struct WeightedSymbol
    {
        public WeightedSymbol(byte symbol, ulong frequency)
        {
            Symbol = symbol;
            Frequency = frequency;
        }

        public byte Symbol { get; }
        public ulong Frequency { get; }
    }

    private readonly struct StateKey : IEquatable<StateKey>
    {
        public StateKey(int used, int slots)
        {
            Used = used;
            Slots = slots;
        }

        public int Used { get; }
        public int Slots { get; }

        public bool Equals(StateKey other)
        {
            return Used == other.Used && Slots == other.Slots;
        }

        public override bool Equals(object? obj)
        {
            return obj is StateKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Used, Slots);
        }
    }

    private readonly struct Parent
    {
        public Parent(StateKey previous, int leavesHere)
        {
            Previous = previous;
            LeavesHere = leavesHere;
        }

        public StateKey Previous { get; }
        public int LeavesHere { get; }
    }

    private readonly struct NodePriority : IComparable<NodePriority>
    {
        public NodePriority(ulong frequency, int order)
        {
            Frequency = frequency;
            Order = order;
        }

        private ulong Frequency { get; }
        private int Order { get; }

        public int CompareTo(NodePriority other)
        {
            var frequencyComparison = Frequency.CompareTo(other.Frequency);
            return frequencyComparison != 0 ? frequencyComparison : Order.CompareTo(other.Order);
        }
    }
}
