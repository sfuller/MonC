using System.Collections.Generic;
using MonC.Semantics.TypeChecks;
using MonC.SyntaxTree.Nodes;
using MonC.TypeSystem;
using MonC.TypeSystem.Types;

namespace MonC.Semantics
{
    public class ExpressionTypeManager
    {
        private readonly SemanticContext _context;
        private readonly TypeManager _typeManager;
        private readonly IErrorManager _errors;

        private readonly Dictionary<IExpressionNode, IType> _expressionResultTypes;

        public ExpressionTypeManager(
            SemanticContext context, TypeManager typeManager, IErrorManager errors,
            Dictionary<IExpressionNode, IType> expressionResultTypes)
        {
            _context = context;
            _typeManager = typeManager;
            _errors = errors;
            _expressionResultTypes = expressionResultTypes;
        }

        public void SetExpressionType(IExpressionNode node, IType type)
        {
            _expressionResultTypes[node] = type;
        }

        public IType GetExpressionType(IExpressionNode node)
        {
            if (!_expressionResultTypes.TryGetValue(node, out IType type)) {
                TypeCheckVisitor visitor = new TypeCheckVisitor(_context, _typeManager, _errors, this);
                node.AcceptExpressionVisitor(visitor);
                type = visitor.Type;
                _expressionResultTypes[node] = type;
            }
            return type;
        }

    }
}
