using System.Collections.Generic;
using MonC.SyntaxTree;
using MonC.SyntaxTree.Nodes;
using MonC.SyntaxTree.Nodes.Expressions;
using MonC.SyntaxTree.Nodes.Statements;
using MonC.SyntaxTree.Util.ChildrenVisitors;

namespace MonC.Parsing.Semantics.TypeAnalysis
{
    public class TypeCheckVisitor : IStatementVisitor, IExpressionVisitor
    {
        public TypeDefinition Type { get; set; }
        private readonly IList<(string message, ISyntaxTreeNode node)> _errors;

        public TypeCheckVisitor(IList<(string message, ISyntaxTreeNode node)> errors)
        {
            _errors = errors;
        }

        private TypeCheckVisitor MakeSubVisitor()
        {
            return new TypeCheckVisitor(_errors);
        }

        public void Process(FunctionDefinitionNode function)
        {
            StatementChildrenVisitor statementVisitor = new StatementChildrenVisitor(this, this);
            function.Body.VisitStatements(statementVisitor);
        }

        public void VisitBody(BodyNode node)
        {
        }

        public void VisitDeclaration(DeclarationNode node)
        {
            if (node.Assignment is VoidExpressionNode) {
                // Ommited assignment.
                return;
            }
            // TODO: Check assignment expression type against declaration type.
            node.Assignment.AcceptExpressionVisitor(this);
        }

        public void VisitIfElse(IfElseNode node)
        {
        }

        public void VisitFor(ForNode node)
        {
        }

        public void VisitWhile(WhileNode node)
        {
        }

        public void VisitBreak(BreakNode node)
        {
        }

        public void VisitContinue(ContinueNode node)
        {
        }

        public void VisitReturn(ReturnNode node)
        {
            // TODO: Verify return value matches function type.
            node.RHS.AcceptExpressionVisitor(this);
        }

        public void VisitExpressionStatement(ExpressionStatementNode node)
        {
            node.Expression.AcceptExpressionVisitor(this);
        }

        public void VisitVoid(VoidExpressionNode node)
        {
            Type = new TypeDefinition("void", PointerType.NotAPointer);
        }

        public void VisitBinaryOperation(IBinaryOperationNode node)
        {
            TypeCheckVisitor lhsTypeCheck = MakeSubVisitor();
            TypeCheckVisitor rhsTypeCheck = MakeSubVisitor();
            node.LHS.AcceptExpressionVisitor(lhsTypeCheck);
            node.RHS.AcceptExpressionVisitor(rhsTypeCheck);

            // For now, both sides must be of same type.
            if (lhsTypeCheck.Type != rhsTypeCheck.Type) {
                _errors.Add(("Type mismatch", node));
            }

            // TODO: Ensure operator is valid based on type.
            Type = lhsTypeCheck.Type;
        }

        public void VisitUnaryOperation(IUnaryOperationNode node)
        {
            // TOOD: Need to account for unary cast operator.
            node.RHS.AcceptExpressionVisitor(this);
            // TODO: Ensure operator is valid based on type.
        }

        public void VisitFunctionCall(FunctionCallNode node)
        {
            Type = new TypeDefinition(node.LHS.ReturnType);
        }

        public void VisitVariable(VariableNode node)
        {
            Type = new TypeDefinition(node.Declaration.Type);
        }

        public void VisitEnumValue(EnumValueNode node)
        {
            Type = new TypeDefinition(node.Enum.Name, PointerType.NotAPointer);
        }

        public void VisitNumericLiteral(NumericLiteralNode node)
        {
            Type = new TypeDefinition("int", PointerType.NotAPointer);
        }

        public void VisitStringLiteral(StringLiteralNode node)
        {
            // String literals are represented as ints, for now.
            Type = new TypeDefinition("int", PointerType.NotAPointer);
        }

        public void VisitAssignment(AssignmentNode node)
        {
            Type = new TypeDefinition(node.Declaration.Type);
            TypeCheckVisitor rhsCheck = MakeSubVisitor();
            node.RHS.AcceptExpressionVisitor(rhsCheck);

            if (Type != rhsCheck.Type) {
                _errors.Add(("Type mismatch", node));
            }
        }

        public void VisitUnknown(IExpressionNode node)
        {
        }
    }
}
