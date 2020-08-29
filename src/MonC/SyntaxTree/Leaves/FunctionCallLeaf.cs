using System;
using System.Collections.Generic;
using System.Linq;

namespace MonC.SyntaxTree
{
    public class FunctionCallLeaf : IASTLeaf
    {
        public FunctionDefinitionLeaf LHS;
        private readonly IASTLeaf[] _arguments;

        public int ArgumentCount => _arguments.Length;

        public FunctionCallLeaf(FunctionDefinitionLeaf lhs, IEnumerable<IASTLeaf> arguments)
        {
            LHS = lhs;
            _arguments = arguments.ToArray();
        }
        
        public void Accept(IASTLeafVisitor visitor)
        {
            visitor.VisitFunctionCall(this);
        }

        public void ReplaceAllArguments(Func<IASTLeaf, IASTLeaf> replacer)
        {
            for (int i = 0, ilen = _arguments.Length; i < ilen; ++i)
                _arguments[i] = replacer(_arguments[i]);
        }

        public IASTLeaf GetArgument(int index)
        {
            return _arguments[index];
        }

        public void SetArgument(int index, IASTLeaf argument)
        {
            _arguments[index] = argument;
        }
    }
}