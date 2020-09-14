using System.Collections.Generic;
using MonC.SyntaxTree;
using MonC.SyntaxTree.Leaves;
using MonC.SyntaxTree.Leaves.Expressions;
using MonC.SyntaxTree.Leaves.Statements;
using MonC.SyntaxTree.Util.ChildrenVisitors;

namespace MonC.Parsing.Semantics.TypeAnalysis
{
    public class TypeCheckVisitor : IStatementVisitor, IExpressionVisitor
    {
        public TypeDefinition Type { get; set; }
        private readonly IList<(string message, ISyntaxTreeLeaf leaf)> _errors;

        public TypeCheckVisitor(IList<(string message, ISyntaxTreeLeaf leaf)> errors)
        {
            _errors = errors;
        }

        private TypeCheckVisitor MakeSubVisitor()
        {
            return new TypeCheckVisitor(_errors);
        }

        public void Process(FunctionDefinitionLeaf function)
        {
            StatementChildrenVisitor statementVisitor = new StatementChildrenVisitor(this, this);
            function.Body.AcceptStatements(statementVisitor);
        }

        public void VisitDeclaration(DeclarationLeaf leaf)
        {
            if (leaf.Assignment is VoidExpression) {
                // Ommited assignment.
                return;
            }
            // TODO: Check assignment expression type against declaration type.
            leaf.Assignment.AcceptExpressionVisitor(this);
        }

        public void VisitIfElse(IfElseLeaf leaf)
        {
        }

        public void VisitFor(ForLeaf leaf)
        {
        }

        public void VisitWhile(WhileLeaf leaf)
        {
        }

        public void VisitBreak(BreakLeaf leaf)
        {
        }

        public void VisitContinue(ContinueLeaf leaf)
        {
        }

        public void VisitReturn(ReturnLeaf leaf)
        {
            // TODO: Verify return value matches function type.
            leaf.RHS.AcceptExpressionVisitor(this);
        }

        public void VisitExpressionStatement(ExpressionStatementLeaf leaf)
        {
            leaf.Expression.AcceptExpressionVisitor(this);
        }

        public void VisitVoid(VoidExpression leaf)
        {
            Type = new TypeDefinition("void", PointerType.NotAPointer);
        }

        public void VisitBinaryOperation(IBinaryOperationLeaf leaf)
        {
            TypeCheckVisitor lhsTypeCheck = MakeSubVisitor();
            TypeCheckVisitor rhsTypeCheck = MakeSubVisitor();
            leaf.LHS.AcceptExpressionVisitor(lhsTypeCheck);
            leaf.RHS.AcceptExpressionVisitor(rhsTypeCheck);

            // For now, both sides must be of same type.
            if (lhsTypeCheck.Type != rhsTypeCheck.Type) {
                _errors.Add(("Type mismatch", leaf));
            }

            // TODO: Ensure operator is valid based on type.
            Type = lhsTypeCheck.Type;
        }

        public void VisitUnaryOperation(IUnaryOperationLeaf leaf)
        {
            // TOOD: Need to account for unary cast operator.
            leaf.RHS.AcceptExpressionVisitor(this);
            // TODO: Ensure operator is valid based on type.
        }

        public void VisitFunctionCall(FunctionCallLeaf leaf)
        {
            Type = new TypeDefinition(leaf.LHS.ReturnType);
        }

        public void VisitVariable(VariableLeaf leaf)
        {
            Type = new TypeDefinition(leaf.Declaration.Type);
        }

        public void VisitEnumValue(EnumValueLeaf leaf)
        {
            Type = new TypeDefinition(leaf.Enum.Name, PointerType.NotAPointer);
        }

        public void VisitNumericLiteral(NumericLiteralLeaf leaf)
        {
            Type = new TypeDefinition("int", PointerType.NotAPointer);
        }

        public void VisitStringLiteral(StringLiteralLeaf leaf)
        {
            // String literals are represented as ints, for now.
            Type = new TypeDefinition("int", PointerType.NotAPointer);
        }

        public void VisitAssignment(AssignmentLeaf leaf)
        {
            Type = new TypeDefinition(leaf.Declaration.Type);
            TypeCheckVisitor rhsCheck = MakeSubVisitor();
            leaf.RHS.AcceptExpressionVisitor(rhsCheck);

            if (Type != rhsCheck.Type) {
                _errors.Add(("Type mismatch", leaf));
            }
        }

        public void VisitUnknown(IExpressionLeaf leaf)
        {
        }
    }
}
