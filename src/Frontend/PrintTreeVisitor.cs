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
            VisitSubleaf(leaf.ElseBody);
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
            throw new NotImplementedException();
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

        public void VisitEnum(EnumLeaf leaf)
        {
            Print("Enum");
        }

        public void VisitEnumValue(EnumValueLeaf leaf)
        {
            Print($"EnumValue (name={leaf.Name})");
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

        public void VisitSubleaf(Optional<IASTLeaf> leaf)
        {
            IASTLeaf leafData;
            if (leaf.Get(out leafData)) {
                VisitSubleaf(leafData);
            }
        }
        
        private void Print(string text)
        {
            Console.WriteLine(new string(' ', _currentIndent) + text);
        }
        
    }
}