using System.Collections.Generic;
using System.Linq;

namespace MonC.SyntaxTree
{
    public class IfElseLeaf : IASTLeaf
    {
        public readonly IASTLeaf Condition;
        public readonly IASTLeaf IfBody;
        public readonly IASTLeaf ElseBody;

        public IfElseLeaf(IASTLeaf condition, IASTLeaf ifBody, IASTLeaf elseBody)
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