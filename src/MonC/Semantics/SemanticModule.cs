using System.Collections.Generic;
using MonC.Parsing;
using MonC.SyntaxTree.Nodes;
using MonC.TypeSystem.Types;

namespace MonC.Semantics
{
    public class SemanticModule
    {
        public readonly ParseModule BaseModule;
        public readonly Dictionary<IExpressionNode, IType> ExpressionResultTypes;

        public SemanticModule(ParseModule baseModule, Dictionary<IExpressionNode, IType> expressionResultTypes)
        {
            BaseModule = baseModule;
            ExpressionResultTypes = expressionResultTypes;
        }
    }
}
