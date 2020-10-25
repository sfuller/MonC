using System;
using MonC.Codegen;
using MonC.SyntaxTree.Nodes.Expressions;
using MonC.TypeSystem.Types.Impl;

namespace MonC.LLVM
{
    internal class AssignmentCodeGenVisitor : IAssignableVisitor
    {
        private readonly Builder _builder;
        private readonly CodeGeneratorContext _genContext;
        private readonly CodeGeneratorContext.Function _function;

        public Value AssignmentWritePointer { get; private set; }

        public AssignmentCodeGenVisitor(
            Builder builder,
            CodeGeneratorContext genContext,
            CodeGeneratorContext.Function function)
        {
            _builder = builder;
            _genContext = genContext;
            _function = function;
        }

        public void VisitVariable(VariableNode node)
        {
            AssignmentWritePointer = _function.VariableValues[node.Declaration];
        }

        public void VisitAccess(AccessNode node)
        {
            IAssignableNode assignableLhs = (IAssignableNode) node.Lhs;
            assignableLhs.AcceptAssignableVisitor(this);

            StructType structType = (StructType) _genContext.SemanticModule.ExpressionResultTypes[node.Lhs];
            Type llvmStructType = _genContext.LookupType(structType)!.Value;
            StructLayout layout = _genContext.StructLayoutManager.GetLayout(structType);
            if (!layout.MemberOffsets.TryGetValue(node.Rhs, out int index)) {
                throw new InvalidOperationException();
            }

            AssignmentWritePointer =
                _builder.BuildStructGEP(llvmStructType, AssignmentWritePointer, (uint) index);
        }
    }
}
