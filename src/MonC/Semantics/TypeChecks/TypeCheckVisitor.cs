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
    public class TypeCheckVisitor : IStatementVisitor, IExpressionVisitor, ISpecifierVisitor, IBasicExpressionVisitor
    {
        private readonly SemanticContext _context;
        private readonly TypeManager _typeManager;
        private readonly IErrorManager _errors;
        private readonly ExpressionTypeManager _expressionTypeManager;

        private readonly SyntaxTreeDelegator _delegator = new SyntaxTreeDelegator();

        public TypeCheckVisitor(SemanticContext context, TypeManager typeManager, IErrorManager errors, ExpressionTypeManager expressionTypeManager)
        {
            _context = context;
            _typeManager = typeManager;
            _errors = errors;
            _expressionTypeManager = expressionTypeManager;

            _delegator.StatementVisitor = this;
            _delegator.ExpressionVisitor = this;
            _delegator.SpecifierVisitor = this;

            // TODO: Better way to get this void type.
            Type = _typeManager.GetType("void", PointerMode.NotAPointer)!;
        }

        public IType Type { get; set; }

        public void SetAndCacheType(IExpressionNode node, IType type)
        {
            Type = type;
            _expressionTypeManager.SetExpressionType(node, type);
        }

        private IType GetExpressionType(IExpressionNode node)
        {
            return _expressionTypeManager.GetExpressionType(node);
        }

        public void Process(FunctionDefinitionNode function)
        {
            SyntaxTreeDelegator childrenDelegator = new SyntaxTreeDelegator();
            StatementChildrenVisitor statementVisitor = new StatementChildrenVisitor(_delegator, childrenDelegator);
            childrenDelegator.StatementVisitor = statementVisitor;
            childrenDelegator.ExpressionVisitor = this;
            function.Body.VisitStatements(statementVisitor);
        }

        public void VisitBody(BodyNode node)
        {
        }

        public void VisitDeclaration(DeclarationNode node)
        {
            node.Type.AcceptSpecifierVisitor(this);
            IType rhsType = GetExpressionType(node.Assignment);

            if (node.Assignment is VoidExpressionNode) {
                // VoidExpression as assignment means assignment is skpped and assignment doesn't take place.
                return;
            }

            if (Type != rhsType) {
                AddAssigmentTypeMismatchError(Type, rhsType, node);
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
            // TODO: Verify return value matches function type. (This should be done at an outer level)
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
            IType lhsType = GetExpressionType(node.LHS);
            IType rhsType = GetExpressionType(node.RHS);

            // Both sides must be of same type. (No implicit conversions in MonC)
            if (lhsType != rhsType) {
                string message = "Type mismatch between binary operator.\n" +
                                 $"  LHS: {lhsType.Represent()}\n" +
                                 $"  RHS: {rhsType.Represent()}";
                _errors.AddError(message, node);
            }

            // TODO: Ensure operator is valid based on type.

            SetAndCacheType(node, lhsType);
        }

        public void VisitBasicExpression(IBasicExpression node)
        {
            node.AcceptBasicExpressionVisitor(this);
        }

        public void VisitUnaryOperation(IUnaryOperationNode node)
        {
            if (node is CastUnaryOpNode castNode) {
                castNode.ToType.AcceptSpecifierVisitor(this);
                SetAndCacheType(node, Type);
                return;
            }

            // TODO: Ensure operator is valid based on type.

            node.RHS.AcceptExpressionVisitor(this);
            SetAndCacheType(node, Type);
        }

        public void VisitFunctionCall(FunctionCallNode node)
        {
            node.LHS.ReturnType.AcceptSyntaxTreeVisitor(_delegator);

            for (int i = 0, ilen = node.Arguments.Count; i < ilen; ++i) {
                DeclarationNode parameter = node.LHS.Parameters[i];
                IExpressionNode argument = node.Arguments[i];

                // TypeSpecifiers should be resolved at this point.
                TypeSpecifierNode typeSpecifier = (TypeSpecifierNode) parameter.Type;
                IType parameterType = typeSpecifier.Type;
                IType argumentType = GetExpressionType(argument);

                if (parameterType != argumentType) {
                    string message = $"Type mismatch between parameter and positional argument {i}.\n" +
                                     $"  Parameter: {parameterType.Represent()}\n" +
                                     $"  Argument: {argumentType.Represent()}";
                    _errors.AddError(message, node);
                }
            }

            SetAndCacheType(node, Type);
        }

        public void VisitVariable(VariableNode node)
        {
            node.Declaration.Type.AcceptSyntaxTreeVisitor(_delegator);
            SetAndCacheType(node, Type);
        }

        public void VisitEnumValue(EnumValueNode node)
        {
            if (!_context.EnumInfo.TryGetValue(node.Declaration.Name, out EnumDeclarationInfo info)) {
                throw new InvalidOperationException("Enumeration declaration name not known by semantic context.");
            }

            IType? type = _typeManager.GetType(info.Enum.Name, PointerMode.NotAPointer);
            if (type == null) {
                throw new InvalidOperationException($"Enum with name {info.Enum.Name} is not registered with the type system.");
            }

            SetAndCacheType(node, type);
        }

        public void VisitNumericLiteral(NumericLiteralNode node)
        {
            IType? type = _typeManager.GetType("int", PointerMode.NotAPointer);
            if (type == null) {
                throw new InvalidOperationException("Primitive types not registered with type system.");
            }

            SetAndCacheType(node, type);
        }

        public void VisitStringLiteral(StringLiteralNode node)
        {
            // String literals are represented as ints, for now.

            IType? type = _typeManager.GetType("int", PointerMode.NotAPointer);
            if (type == null) {
                throw new InvalidOperationException("Primitive types not registered with type system.");
            }

            SetAndCacheType(node, type);
        }

        public void VisitAssignment(AssignmentNode node)
        {
            IType lhsType = GetExpressionType(node.Lhs);
            IType rhsType = GetExpressionType(node.Rhs);

            if (lhsType != rhsType) {
                AddAssigmentTypeMismatchError(lhsType, rhsType, node);
            }

            SetAndCacheType(node, lhsType);
        }

        public void VisitAccess(AccessNode node)
        {
            node.Rhs.Type.AcceptSpecifierVisitor(this);
            SetAndCacheType(node, Type);
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
