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

        private Dictionary<IExpressionNode, IType> _typesByExpression = new Dictionary<IExpressionNode, IType>();

        public ExpressionTypeManager(SemanticContext context, TypeManager typeManager, IErrorManager errors)
        {
            _context = context;
            _typeManager = typeManager;
            _errors = errors;
        }

        public void SetExpressionType(IExpressionNode node, IType type)
        {
            _typesByExpression[node] = type;
        }

        public IType GetExpressionType(IExpressionNode node)
        {
            if (!_typesByExpression.TryGetValue(node, out IType type)) {
                TypeCheckVisitor visitor = new TypeCheckVisitor(_context, _typeManager, _errors, this);
                node.AcceptExpressionVisitor(visitor);
                type = visitor.Type;
                _typesByExpression[node] = type;
            }
            return type;
        }

    }
}
