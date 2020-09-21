using System;
using MonC.Parsing.ParseTree;
using MonC.Parsing.ParseTree.Nodes;
using MonC.SyntaxTree;
using MonC.SyntaxTree.Nodes;
using MonC.SyntaxTree.Nodes.Expressions;
using MonC.SyntaxTree.Nodes.Statements;

namespace MonC.Frontend
{
    public class PrintTreeVisitor :
            ITopLevelStatementVisitor, IStatementVisitor, IExpressionVisitor, IParseTreeVisitor
    {
        private int _currentIndent;

        public void VisitBinaryOperation(IBinaryOperationNode node)
        {
            Print($"{node.GetType().Name}");
            VisitSubnode(node.LHS);
            VisitSubnode(node.RHS);
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
            Print($"Declaration (Type={node.Type}, Name={node.Name})");
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
            Print("Variable");
            VisitSubnode(node.Declaration);
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
            VisitSubnode(node.Declaration);
            VisitSubnode(node.RHS);
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
            Print($"EnumValue (Name={node.Name})");
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

        private void VisitSubnode(IStatementNode node)
        {
            ++_currentIndent;
            node.AcceptStatementVisitor(this);
            --_currentIndent;
        }

        private void VisitSubnode(IExpressionNode node)
        {
            ++_currentIndent;
            node.AcceptExpressionVisitor(this);
            --_currentIndent;
        }

        private void Print(string text)
        {
            Console.WriteLine( new string(' ', _currentIndent * 2) + text);
        }

    }
}
