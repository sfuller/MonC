using System;
using MonC.Semantics;
using MonC.SyntaxTree.Nodes.Expressions;
using MonC.TypeSystem.Types.Impl;

namespace MonC.Codegen
{
    public class AssignmentCodeGenVisitor : IAssignableVisitor
    {
        private readonly FunctionStackLayout _layout;
        private readonly SemanticModule _module;
        private readonly StructLayoutManager _structLayoutManager;

        public int AssignmentWriteLocation { get; private set; }

        public AssignmentCodeGenVisitor(
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
            AssignmentWriteLocation = _layout.Variables[node.Declaration];
        }

        public void VisitAccess(AccessNode node)
        {
            IAssignableNode assignableLhs = (IAssignableNode) node.Lhs;
            assignableLhs.AcceptAssignableVisitor(this);

            StructType structType = (StructType) _module.ExpressionResultTypes[node.Lhs];
            StructLayout layout = _structLayoutManager.GetLayout(structType);
            if (!layout.MemberOffsets.TryGetValue(node.Rhs, out int offset)) {
                throw new InvalidOperationException();
            }

            AssignmentWriteLocation += offset;
        }
    }
}
