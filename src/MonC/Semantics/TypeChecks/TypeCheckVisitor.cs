using System;
using MonC.SyntaxTree;
using MonC.SyntaxTree.Nodes;
using MonC.SyntaxTree.Nodes.Expressions;
using MonC.SyntaxTree.Nodes.Specifiers;
using MonC.SyntaxTree.Nodes.Statements;
using MonC.SyntaxTree.Util.ChildrenVisitors;
using MonC.TypeSystem;
using MonC.TypeSystem.Types;

namespace MonC.Semantics.TypeChecks
{
    public class TypeCheckVisitor : ISyntaxTreeVisitor, IStatementVisitor, IExpressionVisitor
    {
        private readonly TypeManager _typeManager;
        private readonly IErrorManager _errors;

        public TypeCheckVisitor(TypeManager typeManager, IErrorManager errors)
        {
            _typeManager = typeManager;
            _errors = errors;
        }

        public IType? Type { get; set; }

        private TypeCheckVisitor MakeSubVisitor()
        {
            return new TypeCheckVisitor(_typeManager, _errors);
        }

        public void Process(FunctionDefinitionNode function)
        {
            StatementChildrenVisitor statementVisitor = new StatementChildrenVisitor(this);
            function.Body.VisitStatements(statementVisitor);
        }

        public void VisitTopLevelStatement(ITopLevelStatementNode node)
        {
        }

        public void VisitStatement(IStatementNode node)
        {
            node.AcceptStatementVisitor(this);
        }

        public void VisitExpression(IExpressionNode node)
        {
            node.AcceptExpressionVisitor(this);
        }

        public void VisitSpecifier(ISpecifierNode node)
        {
            if (node is TypeSpecifierNode typeSpecifierNode) {
                Type = typeSpecifierNode.Type;
            }
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
            Type = _typeManager.GetType("void")!; // TODO: Better way of managing primitive types.
        }

        public void VisitBinaryOperation(IBinaryOperationNode node)
        {
            TypeCheckVisitor lhsTypeCheck = MakeSubVisitor();
            TypeCheckVisitor rhsTypeCheck = MakeSubVisitor();
            node.LHS.AcceptExpressionVisitor(lhsTypeCheck);
            node.RHS.AcceptExpressionVisitor(rhsTypeCheck);

            if (lhsTypeCheck.Type == null || rhsTypeCheck.Type == null) {
                return;
            }

            // For now, both sides must be of same type.
            if (lhsTypeCheck.Type != rhsTypeCheck.Type) {
                string message = "Type mismatch between binary operator.\n" +
                                 $"  LHS: {lhsTypeCheck.Type.Represent()}\n" +
                                 $"  RHS: {rhsTypeCheck.Type.Represent()}";
                _errors.AddError(message, node);
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
            node.LHS.ReturnType.AcceptSyntaxTreeVisitor(this);
        }

        public void VisitVariable(VariableNode node)
        {
            node.Declaration.Type.AcceptSyntaxTreeVisitor(this);
        }

        public void VisitEnumValue(EnumValueNode node)
        {
            IType? type = _typeManager.GetType(node.Enum.Name, PointerMode.NotAPointer);
            if (type == null) {
                _errors.AddError($"Enum with name {node.Enum.Name} is not registered with the type system.", node.Enum);
                return;
            }
            Type = type;
        }

        public void VisitNumericLiteral(NumericLiteralNode node)
        {
            IType? type = _typeManager.GetType("int", PointerMode.NotAPointer);
            if (type == null) {
                throw new InvalidOperationException("Primitive types not registered with type system.");
            }
            Type = type;
        }

        public void VisitStringLiteral(StringLiteralNode node)
        {
            // String literals are represented as ints, for now.

            IType? type = _typeManager.GetType("int", PointerMode.NotAPointer);
            if (type == null) {
                throw new InvalidOperationException("Primitive types not registered with type system.");
            }
            Type = type;
        }

        public void VisitAssignment(AssignmentNode node)
        {
            VisitSpecifier(node.Declaration.Type);
            TypeCheckVisitor rhsCheck = MakeSubVisitor();
            node.RHS.AcceptExpressionVisitor(rhsCheck);

            if (Type == null || rhsCheck.Type == null) {
                return;
            }

            if (Type != rhsCheck.Type) {
                string message = "Type mismatch between assignment operator.\n" +
                                 $"  LHS: {Type.Represent()}\n" +
                                 $"  RHS: {rhsCheck.Type.Represent()}";
                _errors.AddError(message, node);
            }
        }

        public void VisitUnknown(IExpressionNode node)
        {
        }
    }
}
