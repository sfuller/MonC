using System;
using MonC.SyntaxTree;
using MonC.SyntaxTree.Nodes;
using MonC.SyntaxTree.Nodes.Expressions;
using MonC.SyntaxTree.Nodes.Expressions.UnaryOperations;
using MonC.SyntaxTree.Nodes.Specifiers;
using MonC.SyntaxTree.Nodes.Statements;
using MonC.SyntaxTree.Util.ChildrenVisitors;
using MonC.SyntaxTree.Util.Delegators;
using MonC.TypeSystem;
using MonC.TypeSystem.Types;

namespace MonC.Semantics.TypeChecks
{
    public class TypeCheckVisitor : IStatementVisitor, IExpressionVisitor, ISpecifierVisitor
    {
        private readonly TypeManager _typeManager;
        private readonly IErrorManager _errors;

        private readonly SyntaxTreeDelegator _delegator = new SyntaxTreeDelegator();

        public TypeCheckVisitor(TypeManager typeManager, IErrorManager errors)
        {
            _typeManager = typeManager;
            _errors = errors;

            _delegator.StatementVisitor = this;
            _delegator.ExpressionVisitor = this;
            _delegator.SpecifierVisitor = this;
        }

        public IType? Type { get; set; }

        private TypeCheckVisitor MakeSubVisitor()
        {
            return new TypeCheckVisitor(_typeManager, _errors);
        }

        public void Process(FunctionDefinitionNode function)
        {
            StatementChildrenVisitor statementVisitor = new StatementChildrenVisitor(_delegator);
            function.Body.VisitStatements(statementVisitor);
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
            node.Type.AcceptSpecifierVisitor(this);

            TypeCheckVisitor assignmentVisitor = MakeSubVisitor();
            node.Assignment.AcceptExpressionVisitor(assignmentVisitor);

            if (Type == null || assignmentVisitor.Type == null) {
                return;
            }

            if (Type != assignmentVisitor.Type) {
                AddAssigmentTypeMismatchError(Type, assignmentVisitor.Type, node);
            }
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
            if (node is CastUnaryOpNode castNode) {
                castNode.ToType.AcceptSpecifierVisitor(this);
                return;
            }

            node.RHS.AcceptExpressionVisitor(this);
            // TODO: Ensure operator is valid based on type.
        }

        public void VisitFunctionCall(FunctionCallNode node)
        {
            node.LHS.ReturnType.AcceptSyntaxTreeVisitor(_delegator);
        }

        public void VisitVariable(VariableNode node)
        {
            node.Declaration.Type.AcceptSyntaxTreeVisitor(_delegator);
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
                AddAssigmentTypeMismatchError(Type, rhsCheck.Type, node);
            }
        }

        private void AddAssigmentTypeMismatchError(IType lhsType, IType rhsType, ISyntaxTreeNode node)
        {
            string message = "Type mismatch between assignment operator.\n" +
                             $"  LHS: {lhsType.Represent()}\n" +
                             $"  RHS: {rhsType.Represent()}";
            _errors.AddError(message, node);
        }

        public void VisitUnknown(IExpressionNode node)
        {
        }

        public void VisitTypeSpecifier(TypeSpecifierNode node)
        {
            Type = node.Type;
        }

        public void VisitUnknown(ISpecifierNode node)
        {
        }
    }
}
