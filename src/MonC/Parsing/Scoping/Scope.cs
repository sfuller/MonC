using System.Collections.Generic;
using MonC.SyntaxTree;
using MonC.SyntaxTree.Leaves.Statements;

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

        public static Scope New(FunctionDefinitionLeaf function)
        {
            return new Scope {
                Variables = new List<DeclarationLeaf>(function.Parameters)
            };
        }

        public readonly Scope Copy()
        {
            return new Scope {
                Variables = new List<DeclarationLeaf>(Variables)
            };
        }

    }
}
