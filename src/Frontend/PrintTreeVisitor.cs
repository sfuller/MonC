using System;
using MonC.Parsing.ParseTree;
using MonC.Parsing.ParseTree.Nodes;
using MonC.SyntaxTree;
using MonC.SyntaxTree.Nodes;
using MonC.SyntaxTree.Nodes.Expressions;
using MonC.SyntaxTree.Nodes.Specifiers;
using MonC.SyntaxTree.Nodes.Statements;

namespace MonC.Frontend
{
    public class PrintTreeVisitor :
            ISyntaxTreeVisitor, ITopLevelStatementVisitor, IStatementVisitor, IExpressionVisitor, IParseTreeVisitor,
            IBasicExpressionVisitor, ISpecifierVisitor
    {
        private int _currentIndent;

        public void VisitTopLevelStatement(ITopLevelStatementNode node)
        {
            node.AcceptTopLevelVisitor(this);
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
            node.AcceptSpecifierVisitor(this);
        }

        public void VisitStructFunctionAssociation(StructFunctionAssociationNode node)
        {
            Print($"Struct Function Association (Name = {node.Name})");
        }

        public void VisitEnumDeclaration(EnumDeclarationNode node)
        {
            Print($"Enum Declaration (Name = {node.Name})");
        }

        public void VisitUnknown(ISyntaxTreeNode node)
        {
            Print("(Unknown Syntax Tree Node)");
        }

        public void VisitBinaryOperation(IBinaryOperationNode node)
        {
            Print($"{node.GetType().Name}");
            VisitSubnode(node.LHS);
            VisitSubnode(node.RHS);
        }

        public void VisitBasicExpression(IBasicExpression node)
        {
            node.AcceptBasicExpressionVisitor(this);
        }

        public void VisitUnaryOperation(IUnaryOperationNode node)
        {
            Print($"{node.GetType().Name}");
            VisitSubnode(node.RHS);
        }

        public void VisitBody(BodyNode node)
        {
            Print("Body");
            ++_currentIndent;
            node.VisitStatements(this);
            --_currentIndent;
        }

        public void VisitDeclaration(DeclarationNode node)
        {
            Print($"Declaration (Name={node.Name})");
            VisitSubnode(node.Type);
            VisitSubnode(node.Assignment);
        }

        public void VisitFor(ForNode node)
        {
            Print("For");
            VisitSubnode(node.Declaration);
            VisitSubnode(node.Condition);
            VisitSubnode(node.Update);
            VisitBody(node.Body);
        }

        public void VisitFunctionDefinition(FunctionDefinitionNode node)
        {
            Print($"Function Definition ({node.ReturnType} {node.Name})");
            VisitBody(node.Body);
        }

        public void VisitStruct(StructNode node)
        {
            Print($"Struct ({node.Name})");
        }

        public void VisitFunctionCall(FunctionCallNode node)
        {
            Print($"Function Call (name: {node.LHS.Name}, {node.ArgumentCount} arguments)");
            for (int i = 0, ilen = node.ArgumentCount; i < ilen; ++i) {
                VisitSubnode(node.GetArgument(i));
            }
        }

        public void VisitVariable(VariableNode node)
        {
            Print($"Variable (Declaration.Name = {node.Declaration.Name})");
        }

        public void VisitIfElse(IfElseNode node)
        {
            Print("If Else");
            VisitSubnode(node.Condition);
            VisitBody(node.IfBody);
            VisitBody(node.ElseBody);
        }

        public void VisitVoid(VoidExpressionNode node)
        {
            Print("Void Expression");
        }

        public void VisitNumericLiteral(NumericLiteralNode node)
        {
            Print($"Numeric Literal ({node.Value})");
        }

        public void VisitStringLiteral(StringLiteralNode node)
        {
            Print($"String Literal ({node.Value})");
        }

        public void VisitWhile(WhileNode node)
        {
            Print("While");
            VisitSubnode(node.Condition);
            VisitBody(node.Body);
        }

        public void VisitExpressionStatement(ExpressionStatementNode node)
        {
            Print("Expression Statement");
            VisitSubnode(node.Expression);
        }

        public void VisitBreak(BreakNode node)
        {
            Print("Break");
        }

        public void VisitContinue(ContinueNode node)
        {
            Print("Continue");
        }

        public void VisitReturn(ReturnNode node)
        {
            Print("Return");
            VisitSubnode(node.RHS);
        }

        public void VisitAssignment(AssignmentNode node)
        {
            Print("Assignment");
            VisitSubnode(node.Lhs);
            VisitSubnode(node.Rhs);
        }

        public void VisitAccess(AccessNode node)
        {
            Print($"Access (Rhs.Name = {node.Rhs.Name})");
            VisitSubnode(node.Lhs);
        }

        public void VisitUnknown(IExpressionNode node)
        {
            Print($"{node.GetType().Name}");
        }

        public void VisitEnum(EnumNode node)
        {
            Print("Enum");
        }

        public void VisitEnumValue(EnumValueNode node)
        {
            Print($"EnumValue (Declaration.Name = {node.Declaration.Name})");
        }

        public void VisitAssignment(AssignmentParseNode node)
        {
            Print("Assignment (Parse Tree Node)");
            VisitSubnode(node.LHS);
            VisitSubnode(node.RHS);
        }

        public void VisitIdentifier(IdentifierParseNode node)
        {
            Print($"Identifier (Parse Tree Node) (Name={node.Name})");
        }

        public void VisitFunctionCall(FunctionCallParseNode node)
        {
            Print("Function Call (Parse Tree Node)");
            VisitSubnode(node.LHS);
            for (int i = 0, ilen = node.ArgumentCount; i < ilen; ++i) {
                VisitSubnode(node.GetArgument(i));
            }
        }

        public void VisitTypeSpecifier(TypeSpecifierParseNode node)
        {
            Print("Type Specifier (Parse Tree Node)");
        }

        public void VisitStructFunctionAssociation(StructFunctionAssociationParseNode node)
        {
            Print($"Struct Function Association (Parse Node) (Name = {node.Name}, FunctionName = {node.FunctionName})");
        }

        public void VisitDeclarationIdentifier(DeclarationIdentifierParseNode node)
        {
            Print($"Declaration Identifier (Parse Node) (Name = {node.Name})");
        }

        public void VisitAccess(AccessParseNode node)
        {
            Print($"Access Operator (Rhs.Name = {node.Rhs.Name})");
            VisitSubnode(node.Lhs);
        }

        public void VisitTypeSpecifier(TypeSpecifierNode node)
        {
            Print($"Type Specifier (Type = '{node.Type.Represent()}')");
        }

        public void VisitUnknown(ISpecifierNode node)
        {
            Print("(Unknown Specifier Node)");
        }

        private void VisitSubnode(ISyntaxTreeNode node)
        {
            ++_currentIndent;
            node.AcceptSyntaxTreeVisitor(this);
            --_currentIndent;
        }

        private void Print(string text)
        {
            Console.WriteLine( new string(' ', _currentIndent * 2) + text);
        }

    }
}
