using System;

namespace MonC.SyntaxTree
{
    public class StringLIteralLeaf : IASTLeaf
    {
        public readonly string Value;

        public StringLIteralLeaf(string value)
        {
            Value = value;
        }
        
        public void Accept(IASTLeafVisitor visitor)
        {
            visitor.VisitStringLiteral(this);
        }
    }
}