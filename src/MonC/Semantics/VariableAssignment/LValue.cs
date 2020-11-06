using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MonC.SyntaxTree.Nodes.Statements;

namespace MonC.Semantics
{
    public class LValue
    {
        private readonly DeclarationNode[] _path;

        public LValue(DeclarationNode declaration)
        {
            _path = new [] {declaration};
        }

        public LValue(IEnumerable<DeclarationNode> path)
        {
            _path = path.ToArray();
        }

        public bool Covers(LValue other)
        {
            for (int i = 0, ilen = Math.Min(_path.Length, other._path.Length); i < ilen; ++i) {
                DeclarationNode a = _path[i];
                DeclarationNode b = other._path[i];

                if (a != b) {
                    return false;
                }
            }

            return true;
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("LValue(");
            for (int i = 0, ilen = _path.Length; i < ilen; ++i) {
                builder.Append(_path[i].Name);
                if (i < ilen - 1) {
                    builder.Append('.');
                }
            }
            builder.Append(")");
            return builder.ToString();
        }
    }
}
