using System;
using System.Collections.Generic;

namespace MonC.Semantics.Scoping
{
    public struct NodeScopeInfo
    {
        public Scope Scope;
        public int DeclarationIndex;
    }

    public class ScopeManager
    {
        private readonly Dictionary<ISyntaxTreeNode, NodeScopeInfo> _scopes = new Dictionary<ISyntaxTreeNode, NodeScopeInfo>();

        public void SetScope(ISyntaxTreeNode node, Scope scope)
        {
            NodeScopeInfo info = new NodeScopeInfo { Scope = scope, DeclarationIndex = scope.Variables.Count};
            _scopes[node] = info;
        }

        public void ReplaceNode(ISyntaxTreeNode oldNode, ISyntaxTreeNode newNode)
        {
            if (_scopes.TryGetValue(oldNode, out NodeScopeInfo info)) {
                _scopes[newNode] = info;
            }
        }

        public NodeScopeInfo GetScope(ISyntaxTreeNode node)
        {
            if (!_scopes.TryGetValue(node, out NodeScopeInfo scope)) {
                throw new ArgumentException("No scope associated with this node", nameof(node));
            }
            return scope;
        }
    }
}
