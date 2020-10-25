using System;
using MonC.Semantics;
using MonC.SyntaxTree.Nodes.Expressions;
using MonC.TypeSystem.Types.Impl;

namespace MonC.Codegen
{
    public class AddressOfVisitor : IAddressableVisitor
    {
        private readonly FunctionStackLayout _layout;
        private readonly SemanticModule _module;
        private readonly StructLayoutManager _structLayoutManager;

        public int AbsoluteStackAddress { get; private set; }

        public AddressOfVisitor(
            FunctionStackLayout layout,
                SemanticModule module,
                StructLayoutManager structLayoutManager)
        {
            _layout = layout;
            _module = module;
            _structLayoutManager = structLayoutManager;
        }

        public void VisitVariable(VariableNode node)
        {
            AbsoluteStackAddress = _layout.Variables[node.Declaration];
        }

        public void VisitAccess(AccessNode node)
        {
            IAddressableNode addressableLhs = (IAddressableNode) node.Lhs;
            addressableLhs.AcceptAddressableVisitor(this);

            StructType structType = (StructType) _module.ExpressionResultTypes[node.Lhs];
            StructLayout layout = _structLayoutManager.GetLayout(structType);
            if (!layout.MemberOffsets.TryGetValue(node.Rhs, out int offset)) {
                throw new InvalidOperationException();
            }

            AbsoluteStackAddress += offset;
        }
    }
}
