using System.Collections.Generic;
using System.Linq;

namespace MonC.SyntaxTree
{
    public class BodyLeaf : IASTLeaf
    {
        private readonly IASTLeaf[] _statements;

        public int Length {
            get { return _statements.Length; }
        }
        
        public BodyLeaf(IEnumerable<IASTLeaf> statements)
        {
            _statements = statements.ToArray();
        }
        
        public void Accept(IASTLeafVisitor visitor)
        {
            visitor.VisitBody(this);
        }

        public IASTLeaf GetStatement(int index)
        {
            return _statements[index];
        }
           
    }
}