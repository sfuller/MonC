using System.Collections.Generic;
using MonC.SyntaxTree;
using MonC.SyntaxTree.Nodes;
using MonC.SyntaxTree.Nodes.Statements;

namespace MonC.Codegen
{
    public class StackLayoutGenerator : IStatementVisitor
    {
        public Dictionary<DeclarationNode, int> _variables = new Dictionary<DeclarationNode, int>();
        private int _currentOffset;

        public FunctionStackLayout GetLayout()
        {
            var variables = _variables;
            _variables = new Dictionary<DeclarationNode, int>();
            return new FunctionStackLayout(variables);
        }

        public void VisitBody(BodyNode node)
        {
            node.VisitStatements(this);
        }

        public void VisitDeclaration(DeclarationNode node)
        {
            _variables.Add(node, _currentOffset++);
        }

        public void VisitFor(ForNode node)
        {
            node.Declaration.AcceptStatementVisitor(this);
            VisitBody(node.Body);
        }

        public void VisitFunctionDefinition(FunctionDefinitionNode node)
        {
            foreach (DeclarationNode decl in node.Parameters) {
                VisitDeclaration(decl);
            }
            VisitBody(node.Body);
        }

        public void VisitIfElse(IfElseNode node)
        {
            VisitBody(node.IfBody);
            VisitBody(node.ElseBody);
        }

        public void VisitWhile(WhileNode node)
        {
            VisitBody(node.Body);
        }

        public void VisitBreak(BreakNode node)
        {
        }

        public void VisitContinue(ContinueNode node)
        {
        }

        public void VisitReturn(ReturnNode node)
        {
        }

        public void VisitExpressionStatement(ExpressionStatementNode node)
        {
        }
    }
}
