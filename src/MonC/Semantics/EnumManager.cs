using System.Collections.Generic;
using MonC.SyntaxTree;
using MonC.SyntaxTree.Nodes.Expressions;

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
            foreach (EnumDeclarationNode declaration in node.Declarations) {
                if (_map.ContainsKey(declaration.Name)) {
                    // TODO: Fill out more fields of parse error. An error manager class would be nice. Such a class
                    // could have access to the symbol map and fill out location information based on related nodes.
                    _errors.Add(new ParseError { Message = $"Duplicate declaration of symbol ${declaration.Name}" });
                } else {
                    _map.Add(declaration.Name, node);
                }
            }
        }
    }
}
