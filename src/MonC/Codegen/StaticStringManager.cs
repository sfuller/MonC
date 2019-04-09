using System.Collections.Generic;

namespace MonC.Codegen
{
    public class StaticStringManager
    {
        private readonly List<string> _strings = new List<string>();
        private readonly Dictionary<string, int> _offsets = new Dictionary<string, int>();

        private int _nextOffset;

        public int Get(string value)
        {
            int offset;
            
            if (!_offsets.TryGetValue(value, out offset)) {
                offset = _nextOffset;
                _offsets[value] = offset;
                _strings.Add(value);
                
                // +1 to make space for a null terminator.
                _nextOffset += value.Length + 1;
            }

            return offset;
        }
        
    }
}