using System;
using MonC.IL;
using MonC.Semantics;
using MonC.SyntaxTree.Nodes;
using MonC.SyntaxTree.Nodes.Expressions;
using MonC.SyntaxTree.Nodes.Expressions.UnaryOperations;
using MonC.TypeSystem.Types;

namespace MonC.Codegen
{
    public class UnaryOperationCodeGenVisitor : IUnaryOperationVisitor
    {
        private readonly FunctionBuilder _builder;
        private readonly IExpressionVisitor _expressionVisitor;
        private readonly SemanticModule _semanticModule;
        private readonly TypeSizeManager _typeSizeManager;
        private readonly FunctionStackLayout _functionStackLayout;
        private readonly StructLayoutManager _structLayoutManager;

        public UnaryOperationCodeGenVisitor(FunctionBuilder builder, IExpressionVisitor expressionVisitor, SemanticModule semanticModule, TypeSizeManager typeSizeManager, FunctionStackLayout functionStackLayout, StructLayoutManager structLayoutManager)
        {
            _builder = builder;
            _expressionVisitor = expressionVisitor;
            _semanticModule = semanticModule;
            _typeSizeManager = typeSizeManager;
            _functionStackLayout = functionStackLayout;
            _structLayoutManager = structLayoutManager;
        }

        public void VisitNegateUnaryOp(NegateUnaryOpNode node)
        {
            node.RHS.AcceptExpressionVisitor(_expressionVisitor);
            int startAddr = _builder.AddInstruction(OpCode.PUSHWORD, 0);
            _builder.AddInstruction(OpCode.SUB);
            _builder.AddDebugSymbol(startAddr, node);
        }

        public void VisitLogicalNotUnaryOp(LogicalNotUnaryOpNode node)
        {
            node.RHS.AcceptExpressionVisitor(_expressionVisitor);
            int addr = _builder.AddInstruction(OpCode.LNOT);
            _builder.AddDebugSymbol(addr, node);
        }

        public void VisitCastUnaryOp(CastUnaryOpNode node)
        {
            // TODO: Conversions?
            node.RHS.AcceptExpressionVisitor(_expressionVisitor);
        }

        public void VisitBorrowUnaryOp(BorrowUnaryOpNode node)
        {
            IAddressableNode addressableRhs = (IAddressableNode) node.RHS;
            AddressOfVisitor addressOfVisitor = new AddressOfVisitor(_functionStackLayout, _semanticModule, _structLayoutManager);
            addressableRhs.AcceptAddressableVisitor(addressOfVisitor);
            int startAddr = _builder.AddInstruction(OpCode.ADDRESSOF, addressOfVisitor.AbsoluteStackAddress);
            _builder.AddDebugSymbol(startAddr, node);
        }

        public void VisitDereferenceUnaryOp(DereferenceUnaryOpNode node)
        {
            node.RHS.AcceptExpressionVisitor(_expressionVisitor);
            IType referencedType = _semanticModule.ExpressionResultTypes[node];
            int size = _typeSizeManager.GetSize(referencedType);
            int startAddr = _builder.AddInstruction(OpCode.DEREF, size: size);
            _builder.AddDebugSymbol(startAddr, node);
        }
    }
}
