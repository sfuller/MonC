using System.Collections.Generic;
using MonC.SyntaxTree;

namespace MonC.Semantics
{
    public class EnumManager
    {
        private Dictionary<string, EnumNode> _map = new Dictionary<string,EnumNode>();
        private readonly IList<ParseError> _errors;

        public EnumManager(IList<ParseError> errors)
        {
            _errors = errors;
        }

        public EnumNode? GetEnumeration(string identifier)
        {
            EnumNode? node;
            _map.TryGetValue(identifier, out node);
            return node;
        }

        public void RegisterEnum(EnumNode node)
        {
            foreach (KeyValuePair<string, int> enumeration in node.Enumerations) {
                if (_map.TryGetValue(enumeration.Key, out EnumNode existingNode)) {
                    if (!ReferenceEquals(node, existingNode)) {
                        _errors.Add(new ParseError {Message = $"Duplicate declaration of symbol ${enumeration}"});
                    }
                    continue;
                }
                _map.Add(enumeration.Key, node);
            }
        }
    }
}
