using System.Collections.Generic;
using MonC.SyntaxTree;

namespace MonC.Parsing.Scoping
{
    public struct Scope
    {
        public List<DeclarationLeaf> Variables;

        public static Scope New()
        {
            return new Scope {
                Variables = new List<DeclarationLeaf>()
            };
        }

        public Scope Copy()
        {
            return new Scope {
                Variables = new List<DeclarationLeaf>(Variables)
            };
        }

        public bool ContainsVariable(DeclarationLeaf declaration)
        {
            return Variables.Contains(declaration);
        }

    }
}