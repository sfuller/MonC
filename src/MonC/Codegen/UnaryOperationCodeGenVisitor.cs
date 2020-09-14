using MonC.IL;
using MonC.SyntaxTree.Leaves;
using MonC.SyntaxTree.Leaves.Expressions;
using MonC.SyntaxTree.Leaves.Expressions.UnaryOperations;

namespace MonC.Codegen
{
    public class UnaryOperationCodeGenVisitor : IUnaryOperationVisitor
    {
        private readonly FunctionBuilder _builder;
        private readonly IExpressionVisitor _expressionVisitor;

        public UnaryOperationCodeGenVisitor(FunctionBuilder builder, IExpressionVisitor expressionVisitor)
        {
            _builder = builder;
            _expressionVisitor = expressionVisitor;
        }

        public void VisitNegateUnaryOp(NegateUnaryOpLeaf leaf)
        {
            int rhsStackAddress = _builder.AllocTemporaryStackAddress();
            leaf.RHS.AcceptExpressionVisitor(_expressionVisitor);
            int addr = _builder.AddInstruction(OpCode.WRITE, rhsStackAddress);
            _builder.AddInstruction(OpCode.LOAD, 0);
            _builder.AddInstruction(OpCode.SUB, rhsStackAddress);
            _builder.FreeTemporaryStackAddress();
            _builder.AddDebugSymbol(addr, leaf);
        }

        public void VisitLogicalNotUnaryOp(LogicalNotUnaryOpLeaf leaf)
        {
            leaf.RHS.AcceptExpressionVisitor(_expressionVisitor);
            int addr = _builder.AddInstruction(OpCode.LNOT);
            _builder.AddDebugSymbol(addr, leaf);
        }
    }
}
