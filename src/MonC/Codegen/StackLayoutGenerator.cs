using System.Collections.Generic;
using MonC.SyntaxTree;
using MonC.SyntaxTree.Nodes;
using MonC.SyntaxTree.Nodes.Specifiers;
using MonC.SyntaxTree.Nodes.Statements;
using MonC.TypeSystem.Types;

namespace MonC.Codegen
{
    public class StackLayoutGenerator : IStatementVisitor
    {
        private readonly TypeSizeManager? _typeSizeManager;

        public Dictionary<DeclarationNode, int> _variables = new Dictionary<DeclarationNode, int>();
        private int _returnValueSize;
        private int _argumentsSize;
        private int _currentOffset;

        public StackLayoutGenerator(TypeSizeManager? typeSizeManager = null)
        {
            _typeSizeManager = typeSizeManager;
        }

        public FunctionStackLayout GetLayout()
        {
            var variables = _variables;
            _variables = new Dictionary<DeclarationNode, int>();
            return new FunctionStackLayout(variables, _returnValueSize, _argumentsSize, _currentOffset);
        }

        public void VisitBody(BodyNode node)
        {
            node.VisitStatements(this);
        }

        public void VisitDeclaration(DeclarationNode node)
        {
            _variables.Add(node, _currentOffset);
            IType type = ((TypeSpecifierNode) node.Type).Type;

            // LLVM does its own size and offset calculations
            if (_typeSizeManager != null)
                _currentOffset += _typeSizeManager.GetSize(type);
        }

        public void VisitFor(ForNode node)
        {
            node.Declaration.AcceptStatementVisitor(this);
            VisitBody(node.Body);
        }

        public void VisitFunctionDefinition(FunctionDefinitionNode node)
        {
            // Return value
            IType returnType = ((TypeSpecifierNode) node.ReturnType).Type;
            // LLVM does its own size and offset calculations
            _returnValueSize = _typeSizeManager?.GetSize(returnType) ?? 0;
            _currentOffset += _returnValueSize;

            foreach (DeclarationNode decl in node.Parameters) {
                VisitDeclaration(decl);
            }
            _argumentsSize = _currentOffset - _returnValueSize;
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

        public void VisitBreak(BreakNode node) { }

        public void VisitContinue(ContinueNode node) { }

        public void VisitReturn(ReturnNode node) { }

        public void VisitExpressionStatement(ExpressionStatementNode node) { }
    }
}
