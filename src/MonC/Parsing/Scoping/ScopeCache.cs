using System.Collections.Generic;

namespace MonC.Parsing.Scoping
{
    public class ScopeCache
    {
        private readonly Dictionary<IASTLeaf, Scope> _scopes = new Dictionary<IASTLeaf, Scope>();

        public void SetScope(IASTLeaf leaf, Scope scope)
        {
            _scopes[leaf] = scope.Copy();
        }

        public Scope GetScope(IASTLeaf leaf)
        {
            Scope scope;
            if (_scopes.TryGetValue(leaf, out scope)) {
                return scope.Copy();
            }
            return Scope.New();
        }
    }
}