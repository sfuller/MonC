using System.Collections.Generic;
using System.Linq;

namespace MonC.SyntaxTree
{
    public class IfElseLeaf : IASTLeaf
    {
        public IASTLeaf Condition;
        public IASTLeaf IfBody;
        public Optional<IASTLeaf> ElseBody;

        public IfElseLeaf(IASTLeaf condition, IASTLeaf ifBody, Optional<IASTLeaf> elseBody)
        {
            Condition = condition;
            IfBody = ifBody;
            ElseBody = elseBody;
        }
        
        public void Accept(IASTLeafVisitor visitor)
        {
            visitor.VisitIfElse(this);
        }
    }
}