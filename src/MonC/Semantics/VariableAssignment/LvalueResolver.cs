using System.Collections.Generic;
using MonC.SyntaxTree.Nodes.Expressions;
using MonC.SyntaxTree.Nodes.Statements;

namespace MonC.Semantics
{
    public class LvalueResolver : IAddressableVisitor
    {
        private readonly List<DeclarationNode> _path = new List<DeclarationNode>();

        public LValue? Resolve(IAddressableNode node)
        {
            _path.Clear();
            node.AcceptAddressableVisitor(this);
            if (_path.Count == 0) {
                return null;
            }
            return new LValue(_path);
        }

        public void VisitVariable(VariableNode node)
        {
            _path.Add(node.Declaration);
        }

        public void VisitAccess(AccessNode node)
        {
            if (!node.IsAddressable()) {
                return;
            }

            IAddressableNode addressableLhs = (IAddressableNode) node.Lhs;
            addressableLhs.AcceptAddressableVisitor(this);
            _path.Add(node.Rhs);
        }
    }
}
