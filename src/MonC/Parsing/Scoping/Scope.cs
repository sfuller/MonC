using System.Collections.Generic;
using MonC.SyntaxTree;
using MonC.SyntaxTree.Nodes.Statements;

namespace MonC.Parsing.Scoping
{
    public struct Scope
    {
        public List<DeclarationNode> Variables;

        public static Scope New()
        {
            return new Scope {
                Variables = new List<DeclarationNode>()
            };
        }

        public static Scope New(FunctionDefinitionNode function)
        {
            return new Scope {
                Variables = new List<DeclarationNode>(function.Parameters)
            };
        }

        public readonly Scope Copy()
        {
            return new Scope {
                Variables = new List<DeclarationNode>(Variables)
            };
        }

    }
}
