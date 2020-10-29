using System.Collections.Generic;
using MonC.SyntaxTree.Nodes.Statements;

namespace MonC.Semantics.Scoping
{
    public class Scope
    {
        public readonly Scope? Parent;
        public readonly int ParentDeclarationIndex;
        public readonly List<DeclarationNode> Variables = new List<DeclarationNode>();

        public Scope()
        {
        }

        public Scope(Scope parent, int parentDeclarationIndex)
        {
            Parent = parent;
            ParentDeclarationIndex = parentDeclarationIndex;
        }

        public DeclarationNode? FindNearestDeclaration(string identifier, int declarationIndex)
        {
            int foundIndex = Variables.FindIndex(d => d.Name == identifier);
            if (foundIndex >= 0) {
                if (declarationIndex > foundIndex) {
                    return Variables[foundIndex];
                }
            }

            if (Parent != null) {
                return Parent.FindNearestDeclaration(identifier, ParentDeclarationIndex);
            }
            return null;
        }

    }
}
