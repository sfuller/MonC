using System;
using MonC;
using MonC.SyntaxTree;

namespace LexerFrontend
{
    public class PrintTreeVisitor : IASTLeafVisitor
    {
        private int _currentIndent;
        
        public void VisitBinaryOperation(BinaryOperationExpressionLeaf leaf)
        {
            Print($"Binary Operation ({leaf.Op})");
            VisitSubleaf(leaf.LHS);
            VisitSubleaf(leaf.RHS);
        }

        public void VisitBody(BodyLeaf leaf)
        {
            Print("Body");
            for (int i = 0, ilen = leaf.Length; i < ilen; ++i) {
                VisitSubleaf(leaf.GetStatement(i));
            }
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
            VisitSubleaf(leaf.Body);
        }

        public void VisitFunctionDefinition(FunctionDefinitionLeaf leaf)
        {
            Print($"Function Definition ({leaf.ReturnType} {leaf.Name})");
            VisitSubleaf(leaf.Body);
        }

        public void VisitFunctionCall(FunctionCallLeaf leaf)
        {
            Print($"Function Call ({leaf.ArgumentCount} arguments)");
            VisitSubleaf(leaf.LHS);
            for (int i = 0, ilen = leaf.ArgumentCount; i < ilen; ++i) {
                VisitSubleaf(leaf.GetArgument(i));
            }
        }

        public void VisitIdentifier(IdentifierLeaf leaf)
        {
            Print($"Identifier ({leaf.Name})");
        }

        public void VisitIfElse(IfElseLeaf leaf)
        {
            Print("If Else");
            VisitSubleaf(leaf.Condition);
            VisitSubleaf(leaf.IfBody);
            VisitSubleaf(leaf.ElseBody);
        }

        public void VisitNumericLiteral(NumericLiteralLeaf leaf)
        {
            Print($"Numeric Literal ({leaf.Value})");
        }

        public void VisitStringLiteral(StringLIteralLeaf leaf)
        {
            Print($"String Literal ({leaf.Value})");
        }

        public void VisitWhile(WhileLeaf leaf)
        {
            Print("While");
            VisitSubleaf(leaf.Condition);
            VisitSubleaf(leaf.Body);
        }

        private void VisitSubleaf(IASTLeaf leaf)
        {
            if (leaf == null) {
                return;
            }
            
            ++_currentIndent;
            leaf.Accept(this);
            --_currentIndent;
        }

        private void Print(string text)
        {
            Console.WriteLine(new string(' ', _currentIndent) + text);
        }
    }
}