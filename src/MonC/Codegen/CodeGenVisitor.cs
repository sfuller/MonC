using System;
using System.Collections.Generic;
using MonC.Bytecode;
using MonC.SyntaxTree;

namespace MonC.Codegen
{
    public class CodeGenVisitor : IASTLeafVisitor
    {
        private readonly FunctionStackLayout _layout;
        private readonly FunctionManager _functionManager;

        private readonly List<Instruction> _instructions;
        private readonly StaticStringManager _strings = new StaticStringManager();
        
        private readonly Stack<int> _breaks = new Stack<int>();
        
        private bool _expectingFunctionCall;
        
        
        public CodeGenVisitor(FunctionStackLayout layout, List<Instruction> instructions,FunctionManager functionManager)
        {
            _layout = layout;
            _instructions = instructions;
            _functionManager = functionManager;
        }

        private int AddInstruction(OpCode op, int immediate = 0)
        {
            int index = _instructions.Count;
            _instructions.Add(new Instruction(op, immediate));
            return index;
        }

        public void VisitBinaryOperation(BinaryOperationExpressionLeaf leaf)
        {
            // Evaluate left hand side
            leaf.LHS.Accept(this);

            AddInstruction(OpCode.LOADB);
            
            // Evaluate right hand side
            leaf.RHS.Accept(this);


            switch (leaf.Op.Value) {
                case ">":
                case "<":
                case ">=":
                case "<=":
                    GenerateRelationalComparison(leaf.Op);
                    break;
                case "==":
                    AddInstruction(OpCode.CMPE);
                    break;
                case "!=":
                    AddInstruction(OpCode.CMPE);
                    AddInstruction(OpCode.NOT);
                    break;
                default:
                    throw new NotImplementedException();
            }
            
        }

        private void GenerateRelationalComparison(Token token)
        {
            if (token.Value.Contains("=")) {
                AddInstruction(OpCode.CMPLTE);
            } else {
                AddInstruction(OpCode.CMPLT);
            }

            if (token.Value.Contains(">")) {
                AddInstruction(OpCode.NOT);
            }
        }
        
        public void VisitAssignment(AssignmentLeaf leaf)
        {
            leaf.RHS.Accept(this);
            AddInstruction(OpCode.WRITE, _layout.Variables[leaf.Declaration]);
        }
        
        public void VisitBody(BodyLeaf leaf)
        {
            for (int i = 0, ilen = leaf.Length; i < ilen; ++i) {
                IASTLeaf statement = leaf.GetStatement(i);
                statement.Accept(this);
            }
        }

        public void VisitDeclaration(DeclarationLeaf leaf)
        {
            if (leaf.Assignment != null) {
                leaf.Assignment.Accept(this);
                AddInstruction(OpCode.WRITE, _layout.Variables[leaf]);
            }
        }

        public void VisitFor(ForLeaf leaf)
        {
            leaf.Declaration.Accept(this);
            
            // jump straight to condition
            int initialJumpLocation = AddInstruction(OpCode.NOOP);

            // Generate body code
            int bodyLocation = _instructions.Count;
            leaf.Body.Accept(this);
            
            // Generate update code
            leaf.Update.Accept(this);

            // Generate condition code
            int conditionLocation = _instructions.Count;
            leaf.Condition.Accept(this);
            
            // Branch to body if condition met.
            int currentLocation = _instructions.Count;
            AddInstruction(OpCode.JUMPNZ, bodyLocation - currentLocation);
            
            _instructions[initialJumpLocation] = new Instruction(OpCode.JUMP, conditionLocation - initialJumpLocation);
        }

        public void VisitFunctionCall(FunctionCallLeaf leaf)
        {
            for (int i = 0, ilen = leaf.ArgumentCount; i < ilen; ++i) {
                leaf.GetArgument(i).Accept(this);
                AddInstruction(OpCode.PUSHARG);
            }
            
            AddInstruction(OpCode.CALL, _functionManager.GetFunctionIndex(leaf.LHS));
        }
        
        public void VisitFunctionDefinition(FunctionDefinitionLeaf leaf)
        {
            throw new InvalidOperationException("Not expecting a function here!");
        }

        public void VisitVariable(VariableLeaf leaf)
        {
            AddInstruction(OpCode.READ, _layout.Variables[leaf.Declaration]);
        }

        public void VisitIfElse(IfElseLeaf leaf)
        {
            leaf.Condition.Accept(this);
            
            // Make space for the branch instruction we will instert.
            int branchIndex = AddInstruction(OpCode.NOOP);
            
            leaf.IfBody.Accept(this);
            // Jump to end of if/else after evaluation of if body.
            int ifEndIndex = AddInstruction(OpCode.NOOP);

            if (leaf.ElseBody != null) {
                leaf.ElseBody.Accept(this);    
            }

            int endIndex = _instructions.Count;

            _instructions[branchIndex] = new Instruction(OpCode.JUMPZ, ifEndIndex - branchIndex);
            _instructions[ifEndIndex] =  new Instruction(OpCode.JUMP, endIndex - ifEndIndex - 1);
        }

        public void VisitNumericLiteral(NumericLiteralLeaf leaf)
        {
            int value = int.Parse(leaf.Value);
            AddInstruction(OpCode.LOAD, value);
        }

        public void VisitStringLiteral(StringLiteralLeaf leaf)
        {
            throw new NotImplementedException();
            //int offset = _strings.Get(leaf.Value);
            //AddInstruction(new PushDataInstruction(offset));
        }

        public void VisitWhile(WhileLeaf leaf)
        {
            // jump straight to condition
            int initialJumpLocation = AddInstruction(OpCode.NOOP);

            // Generate body code
            int bodyLocation = _instructions.Count;
            leaf.Body.Accept(this);

            // Generate condition code
            int conditionLocation = _instructions.Count;
            leaf.Condition.Accept(this);
            
            // Branch to body if condition met.
            int currentLocation = _instructions.Count;
            AddInstruction(OpCode.JUMPNZ, bodyLocation - currentLocation);
            
            _instructions[initialJumpLocation] = new Instruction(OpCode.JUMP, conditionLocation - initialJumpLocation);
        }

        public void VisitBreak(BreakLeaf leaf)
        {
            int breakIndex = AddInstruction(OpCode.NOOP);
            _breaks.Push(breakIndex);
        }

        public void VisitReturn(ReturnLeaf leaf)
        {
            if (leaf.RHS != null) {
                leaf.RHS.Accept(this);
            }
            AddInstruction(OpCode.RETURN);
        }
    }
}