using System;
using LLVMSharp.Interop;
using MonC.SyntaxTree.Nodes.Expressions;
using MonC.SyntaxTree.Nodes.Expressions.UnaryOperations;

namespace MonC.LLVM
{
    public readonly struct UnaryOperationCodeGenVisitor : IUnaryOperationVisitor
    {
        private readonly FunctionCodeGenVisitor _codeGenVisitor;

        public UnaryOperationCodeGenVisitor(FunctionCodeGenVisitor codeGenVisitor) => _codeGenVisitor = codeGenVisitor;

        public void VisitNegateUnaryOp(NegateUnaryOpNode node)
        {
            _codeGenVisitor._visitedValue = _codeGenVisitor._builder.BuildNeg(GetUnaryOperand(node));
        }

        public void VisitLogicalNotUnaryOp(LogicalNotUnaryOpNode node)
        {
            _codeGenVisitor._visitedValue = _codeGenVisitor.ConvertToBool(GetUnaryOperand(node), true);
        }

        public void VisitCastUnaryOp(CastUnaryOpNode node)
        {
            Value operand = GetUnaryOperand(node);
            Type destTp = _codeGenVisitor._genContext.LookupType(node.ToType)!.Value;
            LLVMOpcode castOp = _codeGenVisitor.GetCastOpcode(operand, destTp);
            _codeGenVisitor._visitedValue = _codeGenVisitor._builder.BuildCast(castOp, operand, destTp);
        }

        public void VisitBorrowUnaryOp(BorrowUnaryOpNode node)
        {
            throw new NotImplementedException();
        }

        public void VisitDereferenceUnaryOp(DereferenceUnaryOpNode node)
        {
            throw new NotImplementedException();
        }

        private Value GetUnaryOperand(IUnaryOperationNode node)
        {
            node.RHS.AcceptExpressionVisitor(_codeGenVisitor);
            Value rhs = _codeGenVisitor._visitedValue;
            if (!rhs.IsValid) {
                throw new InvalidOperationException("RHS did not produce a usable rvalue");
            }

            _codeGenVisitor.SetCurrentDebugLocation(node);
            return rhs;
        }
    }
}
