using System.Collections.Generic;
using MonC.VM;

namespace CoreLib
{
    /// <summary>
    /// Very naive implementation of a memory heap.
    /// </summary>
    [LinkableModule]
    public static class Memory
    {
        // A tribute to all party players across the globe.
        private const int UNDEFINED_VALUE = -559038737;
        
        private static readonly List<int> _heap = new List<int>();
        private static readonly SortedList<int, int> _holes = new SortedList<int, int>();
        private static readonly Dictionary<int, int> _allocations = new Dictionary<int, int>();

        [LinkableFunction(ArgumentCount = 1)]
        public static int malloc(int[] args)
        {
            int size = args[0];
            int address = 0;
            
            // Allocate within a hole
            foreach (KeyValuePair<int, int> hole in _holes) {
                if (hole.Value < size) {
                    continue;
                }

                address = hole.Key;

                _holes.Remove(hole.Key);
                
                if (hole.Value != size) {
                    _holes.Add(address + size, hole.Value - size);
                }

                break;
            }

            // No holes to fill, allocate at end of heap.
            if (address == 0) {
                address = _heap.Count;
                _heap.Capacity = address + size;
                for (int i = 0; i < size; ++i) {
                    _heap.Add(UNDEFINED_VALUE);  
                }
            }
            
            _allocations.Add(address, size);

            return address;
        }
        
        [LinkableFunction(ArgumentCount = 1)]
        public static int free(int[] args)
        {
            int address = args[0];
            int size;
            
            if (!_allocations.TryGetValue(address, out size)) {
                // This is a death sentence. May god have mercy on your soul.
                return 1;
            }
            
            // Make a hole
            _holes.Add(address, size);
            
            // Merge previous hole
            int holeIndex = _holes.IndexOfKey(address);
            if (holeIndex > 0) {
                int previousHoleIndex = holeIndex - 1;
                int previousHoleAddress = _holes.Keys[previousHoleIndex];
                int previousHoleSize = _holes.Values[previousHoleAddress];
                
                if (previousHoleAddress + previousHoleSize >= address) {
                    _holes.RemoveAt(previousHoleIndex);
                    _holes.Remove(address);
                    _holes.Add(previousHoleAddress, previousHoleSize + size);
                    holeIndex = previousHoleIndex;
                }
            }

            // Merge next hole
            if (holeIndex < _holes.Count - 1) {
                int nextHoleIndex = holeIndex + 1;
                int nextHoleAddress = _holes.Keys[nextHoleIndex];

                if (address + size >= nextHoleAddress) {
                    int nextHoleSize = _holes.Values[nextHoleIndex];
                    _holes.RemoveAt(nextHoleIndex);
                    _holes.Remove(address);
                    _holes.Add(address, size + nextHoleSize);
                }
            }

            return 0;
        }

        [LinkableFunction(ArgumentCount = 3)]
        public static int memset(int[] args)
        {
            int dest = args[0];
            int value = args[1];
            int size = args[2];
            
            while (size-- > 0) {
                _heap[dest++] = value;
            }
            
            return 0;
        }

        [LinkableFunction(ArgumentCount = 2)]
        public static int poke(int[] args)
        {
            int dest = args[0];
            int value = args[1];
            
            _heap[dest] = value;
            return 0;
        }

        [LinkableFunction(ArgumentCount = 1)]
        public static int peek(int[] args)
        {
            int src = args[0];
            return _heap[src];
        }
    }
}