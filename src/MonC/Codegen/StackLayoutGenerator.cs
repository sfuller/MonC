using System.Collections.Generic;
using MonC.SyntaxTree;
using MonC.SyntaxTree.Leaves;
using MonC.SyntaxTree.Leaves.Statements;

namespace MonC.Codegen
{
    public class StackLayoutGenerator : IStatementVisitor
    {
        public Dictionary<DeclarationLeaf, int> _variables = new Dictionary<DeclarationLeaf, int>();
        private int _currentOffset;

        public FunctionStackLayout GetLayout()
        {
            var variables = _variables;
            _variables = new Dictionary<DeclarationLeaf, int>();
            return new FunctionStackLayout(variables);
        }

        public void VisitBody(BodyLeaf leaf)
        {
            leaf.VisitStatements(this);
        }

        public void VisitDeclaration(DeclarationLeaf leaf)
        {
            _variables.Add(leaf, _currentOffset++);
        }

        public void VisitFor(ForLeaf leaf)
        {
            leaf.Declaration.AcceptStatementVisitor(this);
            VisitBody(leaf.Body);
        }

        public void VisitFunctionDefinition(FunctionDefinitionLeaf leaf)
        {
            foreach (DeclarationLeaf decl in leaf.Parameters) {
                VisitDeclaration(decl);
            }
            VisitBody(leaf.Body);
        }

        public void VisitIfElse(IfElseLeaf leaf)
        {
            VisitBody(leaf.IfBody);
            VisitBody(leaf.ElseBody);
        }

        public void VisitWhile(WhileLeaf leaf)
        {
            VisitBody(leaf.Body);
        }

        public void VisitBreak(BreakLeaf leaf)
        {
        }

        public void VisitContinue(ContinueLeaf leaf)
        {
        }

        public void VisitReturn(ReturnLeaf leaf)
        {
        }

        public void VisitExpressionStatement(ExpressionStatementLeaf leaf)
        {
        }
    }
}
