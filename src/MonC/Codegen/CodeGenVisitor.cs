using System;
using System.Collections.Generic;
using System.Data.Common;
using MonC.Bytecode;
using MonC.SyntaxTree;

namespace MonC.Codegen
{
    public class CodeGenVisitor : IASTLeafVisitor
    {
//        struct IdentifierContext
//        {
//            public IdentifierType Type;
//        }

        enum IdentifierType
        {
            NONE,
            VALUE,
            FUNCTION
        }
        
        struct StackEntry
        {
            public string Name;
            public int Size; 
        }
        
        private readonly List<IInstruction> _instructions = new List<IInstruction>();
        private readonly Stack<LocalVariableContext> _localVariableContexts = new Stack<LocalVariableContext>();
        private readonly StaticStringManager _strings = new StaticStringManager();
        
        private IdentifierType _currentIdentifier;

        private bool _expectingFunctionCall;
        

        private int AddInstruction(IInstruction instruction)
        {
            int index = _instructions.Count;
            _instructions.Add(instruction);
            return index;
        }

        private LocalVariableContext GetCurrentLocalVariableContext()
        {
            return _localVariableContexts.Peek();
        }
        
        
       
        public void VisitBinaryOperation(BinaryOperationExpressionLeaf leaf)
        {

            if (leaf.Op.Value == "(") {
                
            }
            
            
            
            
            // Evaluate left hand side
            leaf.LHS.Accept(this);
            
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
                    AddInstruction(new CompareEqualityInstruction());
                    break;
                default:
                    throw new NotImplementedException();
            }
            
        }

        private void GenerateRelationalComparison(Token token)
        {
            if (token.Value.Contains("=")) {
                AddInstruction(new CompareLTEInstruction());
            } else {
                AddInstruction(new CompareLTInstruction());
            }

            if (token.Value.Contains(">")) {
                AddInstruction(new NotInstruction());
            }
        }

        public void VisitBody(BodyLeaf leaf)
        {
            throw new NotImplementedException();
        }

        public void VisitDeclaration(DeclarationLeaf leaf)
        {
            LocalVariableContext context = GetCurrentLocalVariableContext();
            LocalVariable var;
            if (context.GetVariable(leaf.Name, out var)) {
                // TODO: Add an error!
                throw new NotImplementedException(); 
            }
            
            context.AddVariable(leaf.Name, var);

            if (leaf.Assignment != null) {
                // Evaluate assignment expression
                leaf.Assignment.Accept(this);

                // Store the result of the expression
                AddInstruction(new PushLocalInstruction());
                AddInstruction(new StoreInstruction());
            }
        }

        public void VisitFor(ForLeaf leaf)
        {
            leaf.Declaration.Accept(this);
            
            // jump straight to condition
            AddInstruction(new PushImmediateInstruction(1));
            int initialJumpLocation = AddInstruction(null);

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
            AddInstruction(new BranchInstruction(bodyLocation - currentLocation));
            
            _instructions[initialJumpLocation] = new BranchInstruction(conditionLocation - initialJumpLocation);
        }

        public void VisitFunctionCall(FunctionCallLeaf leaf)
        {
            CallVisitor visitor = new CallVisitor();
            leaf.LHS.Accept(visitor);
            throw new NotImplementedException();
        }
        
        public void VisitFunctionDefinition(FunctionDefinitionLeaf leaf)
        {
            throw new InvalidOperationException("Not expecting a function here!");
        }

        public void VisitIdentifier(IdentifierLeaf leaf)
        {
            throw new System.NotImplementedException();
        }

        public void VisitIfElse(IfElseLeaf leaf)
        {
            leaf.Condition.Accept(this);
            
            // Make space for the branch instruction we will instert.
            int branchIndex = AddInstruction(null);
            
            leaf.IfBody.Accept(this);
            // Jump to end of if/else after evaluation of if body.
            AddInstruction(new PushImmediateInstruction(1));
            int ifEndIndex = AddInstruction(null);
            
            leaf.ElseBody.Accept(this);

            int endIndex = _instructions.Count;

            BranchInstruction branchToElseInstr = new BranchInstruction(ifEndIndex - branchIndex);
            BranchInstruction branchToEndInstr = new BranchInstruction(endIndex - ifEndIndex);

            _instructions[branchIndex] = branchToElseInstr;
            _instructions[ifEndIndex] = branchToEndInstr;
        }

        public void VisitNumericLiteral(NumericLiteralLeaf leaf)
        {
            int value = Int32.Parse(leaf.Value);
            AddInstruction(new PushImmediateInstruction(value));
        }

        public void VisitStringLiteral(StringLiteralLeaf leaf)
        {
            int offset = _strings.Get(leaf.Value);
            AddInstruction(new PushDataInstruction(offset));
        }

        public void VisitWhile(WhileLeaf leaf)
        {
            // jump straight to condition
            AddInstruction(new PushImmediateInstruction(1));
            int initialJumpLocation = AddInstruction(null);

            // Generate body code
            int bodyLocation = _instructions.Count;
            leaf.Body.Accept(this);

            // Generate condition code
            int conditionLocation = _instructions.Count;
            leaf.Condition.Accept(this);
            
            // Branch to body if condition met.
            int currentLocation = _instructions.Count;
            AddInstruction(new BranchInstruction(bodyLocation - currentLocation));
            
            _instructions[initialJumpLocation] = new BranchInstruction(conditionLocation - initialJumpLocation);
        }
    }
}