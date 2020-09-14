using System.Collections.Generic;
using MonC.SyntaxTree.Leaves;

namespace MonC.SyntaxTree
{
    public class Body
    {
        private readonly List<IStatementLeaf> _statements;

        public int Length => _statements.Count;

        public Body()
        {
            _statements = new List<IStatementLeaf>();
        }

        public Body(IEnumerable<IStatementLeaf> statements)
        {
            _statements = new List<IStatementLeaf>(statements);
        }

        public IStatementLeaf GetStatement(int index)
        {
            return _statements[index];
        }

        public void SetStatement(int index, IStatementLeaf statement)
        {
            _statements[index] = statement;
        }

        public void AddStatement(IStatementLeaf statement)
        {
            _statements.Add(statement);
        }

        public void InsertStatement(int index, IStatementLeaf statement)
        {
            _statements.Insert(index, statement);
        }

        public void RemoveStatement(int index)
        {
            _statements.RemoveAt(index);
        }

        public void AcceptStatements(IStatementVisitor visitor)
        {
            for (int i = 0, ilen = Length; i < ilen; ++i) {
                GetStatement(i).AcceptStatementVisitor(visitor);
            }
        }

    }
}
