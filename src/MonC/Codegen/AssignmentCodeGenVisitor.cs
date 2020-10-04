using MonC.IL;
using MonC.SyntaxTree.Nodes;
using MonC.SyntaxTree.Nodes.Expressions;

namespace MonC.Codegen
{
    public class AssignmentCodeGenVisitor : IAssignableVisitor
    {
        private readonly AssignmentNode _assignment;
        private readonly FunctionBuilder _functionBuilder;
        private readonly FunctionStackLayout _layout;
        private readonly IExpressionVisitor _expressionVisitor;

        public AssignmentCodeGenVisitor(AssignmentNode assignment, FunctionBuilder functionBuilder, FunctionStackLayout layout, IExpressionVisitor expressionVisitor)
        {
            _assignment = assignment;
            _functionBuilder = functionBuilder;
            _layout = layout;
            _expressionVisitor = expressionVisitor;
        }

        public void VisitVariable(VariableNode node)
        {
            _assignment.Rhs.AcceptExpressionVisitor(_expressionVisitor);
            int variableAddress;
            _layout.Variables.TryGetValue(node.Declaration, out variableAddress);
            int addr = _functionBuilder.AddInstruction(OpCode.WRITE, variableAddress);
            _functionBuilder.AddDebugSymbol(addr, node);
        }

        public void VisitAccess(AccessNode node)
        {
            throw new System.NotImplementedException();
        }
    }
}
