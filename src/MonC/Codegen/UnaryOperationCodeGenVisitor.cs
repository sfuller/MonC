using MonC.IL;
using MonC.SyntaxTree.Nodes;
using MonC.SyntaxTree.Nodes.Expressions;
using MonC.SyntaxTree.Nodes.Expressions.UnaryOperations;

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

        public void VisitNegateUnaryOp(NegateUnaryOpNode node)
        {
            //int rhsStackAddress = _builder.AllocTemporaryStackAddress();
            node.RHS.AcceptExpressionVisitor(_expressionVisitor);
            //int addr = _builder.AddInstruction(OpCode.WRITE, rhsStackAddress);
            //_builder.AddInstruction(OpCode.LOAD, 0);
            int startAddr = _builder.AddInstruction(OpCode.PUSH, 0);
            _builder.AddInstruction(OpCode.SUB);
            //_builder.FreeTemporaryStackAddress();
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
    }
}
