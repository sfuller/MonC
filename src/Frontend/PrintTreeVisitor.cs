using System;
using MonC.Parsing;
using MonC.Parsing.ParseTreeLeaves;
using MonC.SyntaxTree;
using MonC.SyntaxTree.Leaves;
using MonC.SyntaxTree.Leaves.Expressions;
using MonC.SyntaxTree.Leaves.Statements;

namespace MonC.Frontend
{
    public class PrintTreeVisitor :
            ITopLevelStatementVisitor, IStatementVisitor, IExpressionVisitor, IParseTreeVisitor
    {
        private int _currentIndent;

        public void VisitBinaryOperation(IBinaryOperationLeaf leaf)
        {
            Print($"{leaf.GetType().Name}");
            VisitSubleaf(leaf.LHS);
            VisitSubleaf(leaf.RHS);
        }

        public void VisitUnaryOperation(IUnaryOperationLeaf leaf)
        {
            Print($"{leaf.GetType().Name}");
            VisitSubleaf(leaf.RHS);
        }

        public void VisitBody(BodyLeaf leaf)
        {
            Print("Body");
            ++_currentIndent;
            leaf.VisitStatements(this);
            --_currentIndent;
        }

        public void VisitDeclaration(DeclarationLeaf leaf)
        {
            Print($"Declaration (Type={leaf.Type}, Name={leaf.Name})");
            VisitSubleaf(leaf.Assignment);
        }

        public void VisitFor(ForLeaf leaf)
        {
            Print("For");
            VisitSubleaf(leaf.Declaration);
            VisitSubleaf(leaf.Condition);
            VisitSubleaf(leaf.Update);
            VisitBody(leaf.Body);
        }

        public void VisitFunctionDefinition(FunctionDefinitionLeaf leaf)
        {
            Print($"Function Definition ({leaf.ReturnType} {leaf.Name})");
            VisitBody(leaf.Body);
        }

        public void VisitFunctionCall(FunctionCallLeaf leaf)
        {
            Print($"Function Call (name: {leaf.LHS.Name}, {leaf.ArgumentCount} arguments)");
            for (int i = 0, ilen = leaf.ArgumentCount; i < ilen; ++i) {
                VisitSubleaf(leaf.GetArgument(i));
            }
        }

        public void VisitVariable(VariableLeaf leaf)
        {
            Print("Variable");
            VisitSubleaf(leaf.Declaration);
        }

        public void VisitIfElse(IfElseLeaf leaf)
        {
            Print("If Else");
            VisitSubleaf(leaf.Condition);
            VisitBody(leaf.IfBody);
            VisitBody(leaf.ElseBody);
        }

        public void VisitVoid(VoidExpression leaf)
        {
            Print("Void Expression");
        }

        public void VisitNumericLiteral(NumericLiteralLeaf leaf)
        {
            Print($"Numeric Literal ({leaf.Value})");
        }

        public void VisitStringLiteral(StringLiteralLeaf leaf)
        {
            Print($"String Literal ({leaf.Value})");
        }

        public void VisitWhile(WhileLeaf leaf)
        {
            Print("While");
            VisitSubleaf(leaf.Condition);
            VisitBody(leaf.Body);
        }

        public void VisitExpressionStatement(ExpressionStatementLeaf leaf)
        {
            Print("Expression Statement");
            VisitSubleaf(leaf.Expression);
        }

        public void VisitBreak(BreakLeaf leaf)
        {
            Print("Break");
        }

        public void VisitContinue(ContinueLeaf leaf)
        {
            Print("Continue");
        }

        public void VisitReturn(ReturnLeaf leaf)
        {
            Print("Return");
            VisitSubleaf(leaf.RHS);
        }

        public void VisitAssignment(AssignmentLeaf leaf)
        {
            Print("Assignment");
            VisitSubleaf(leaf.Declaration);
            VisitSubleaf(leaf.RHS);
        }

        public void VisitUnknown(IExpressionLeaf leaf)
        {
            Print($"{leaf.GetType().Name}");
        }

        public void VisitEnum(EnumLeaf leaf)
        {
            Print("Enum");
        }

        public void VisitEnumValue(EnumValueLeaf leaf)
        {
            Print($"EnumValue (Name={leaf.Name})");
        }

        public void VisitAssignment(AssignmentParseLeaf leaf)
        {
            Print("Assignment (Parse Tree Leaf)");
            VisitSubleaf(leaf.LHS);
            VisitSubleaf(leaf.RHS);
        }

        public void VisitIdentifier(IdentifierParseLeaf leaf)
        {
            Print($"Identifier (Parse Tree Leaf) (Name={leaf.Name})");
        }

        public void VisitFunctionCall(FunctionCallParseLeaf leaf)
        {
            Print("Function Call (Parse Tree Leaf)");
            VisitSubleaf(leaf.LHS);
            for (int i = 0, ilen = leaf.ArgumentCount; i < ilen; ++i) {
                VisitSubleaf(leaf.GetArgument(i));
            }
        }

        private void VisitSubleaf(IStatementLeaf leaf)
        {
            ++_currentIndent;
            leaf.AcceptStatementVisitor(this);
            --_currentIndent;
        }

        private void VisitSubleaf(IExpressionLeaf leaf)
        {
            ++_currentIndent;
            leaf.AcceptExpressionVisitor(this);
            --_currentIndent;
        }

        private void Print(string text)
        {
            Console.WriteLine( new string(' ', _currentIndent * 2) + text);
        }

    }
}
