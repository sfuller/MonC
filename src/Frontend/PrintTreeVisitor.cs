using System;
using MonC.Parsing;
using MonC.Parsing.ParseTreeLeaves;
using MonC.SyntaxTree;

namespace MonC.Frontend
{
    public class PrintTreeVisitor : IASTLeafVisitor, IParseTreeLeafVisitor
    {
        private int _currentIndent;
        
        public void VisitBinaryOperation(BinaryOperationExpressionLeaf leaf)
        {
            Print($"Binary Operation ({leaf.Op})");
            VisitSubleaf(leaf.LHS);
            VisitSubleaf(leaf.RHS);
        }

        public void VisitUnaryOperation(UnaryOperationLeaf leaf)
        {
            Print($"Unary Operation ({leaf.Operator})");
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
            VisitOptionalSubleaf(leaf.Assignment);
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
            Print($"Function Call (name: {leaf.LHS.Name}, {leaf.ArgumentCount} arguments)");
            for (int i = 0, ilen = leaf.ArgumentCount; i < ilen; ++i) {
                VisitSubleaf(leaf.GetArgument(i));
            }
        }

        public void VisitVariable(VariableLeaf leaf)
        {
            Print($"Variable");
            VisitSubleaf(leaf.Declaration);
        }

        public void VisitIfElse(IfElseLeaf leaf)
        {
            Print("If Else");
            VisitSubleaf(leaf.Condition);
            VisitSubleaf(leaf.IfBody);
            VisitOptionalSubleaf(leaf.ElseBody);
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
            VisitSubleaf(leaf.Body);
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
            VisitOptionalSubleaf(leaf.RHS);
        }

        public void VisitAssignment(AssignmentLeaf leaf)
        {
            Print("Assignment");
            VisitSubleaf(leaf.Declaration);
            VisitSubleaf(leaf.RHS);
        }

        public void VisitEnum(EnumLeaf leaf)
        {
            Print("Enum");
        }

        public void VisitEnumValue(EnumValueLeaf leaf)
        {
            Print($"EnumValue (Name={leaf.Name})");
        }

        public void VisitTypeSpecifier(TypeSpecifierLeaf leaf)
        {
            Print($"TypeSpecifier (Name={leaf.Name}, PointerType={leaf.PointerType})");
        }

        public void VisitIdentifier(IdentifierParseLeaf leaf)
        {
            Print($"Identifier (Parse Tree Leaf) (Name={leaf.Name})");
        }

        public void VisitFunctionCall(FunctionCallParseLeaf leaf)
        {
            Print($"Function Call (Parse Tree Leaf)");
            VisitSubleaf(leaf.LHS);
            for (int i = 0, ilen = leaf.ArgumentCount; i < ilen; ++i) {
                VisitSubleaf(leaf.GetArgument(i));
            }
        }

        private void VisitSubleaf(IASTLeaf leaf)
        {
            ++_currentIndent;
            leaf.Accept(this);
            --_currentIndent;
        }

        public void VisitOptionalSubleaf(IASTLeaf? leaf)
        {
            if (leaf == null) {
                return;
            }
            VisitSubleaf(leaf);
        }
        
        private void Print(string text)
        {
            Console.WriteLine(new string(' ', _currentIndent) + text);
        }
        
    }
}