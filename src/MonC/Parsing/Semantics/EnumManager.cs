using System.Collections.Generic;
using MonC.SyntaxTree;

namespace MonC.Parsing.Semantics
{
    public class EnumManager
    {
        private Dictionary<string, EnumLeaf> _map = new Dictionary<string,EnumLeaf>();
        private readonly IList<ParseError> _errors;

        public EnumManager(IList<ParseError> errors)
        {
            _errors = errors;
        }

        public EnumLeaf? GetEnumeration(string identifier)
        {
            EnumLeaf? leaf;
            _map.TryGetValue(identifier, out leaf);
            return leaf;
        }
        
        public void RegisterEnum(EnumLeaf leaf)
        {
            foreach (KeyValuePair<string, int> enumeration in leaf.Enumerations) {
                if (_map.ContainsKey(enumeration.Key)) {
                    _errors.Add(new ParseError { Message = $"Duplicate declaration of symbol ${enumeration}" });
                    continue;
                }
                _map.Add(enumeration.Key, leaf);
            }
        }
    }
}