using System.Collections.Generic;

namespace MonC.SyntaxTree.Leaves.Statements
{
    public class BodyLeaf : IStatementLeaf
    {
        public readonly List<IStatementLeaf> Statements;

        public BodyLeaf()
        {
            Statements = new List<IStatementLeaf>();
        }

        public BodyLeaf(IEnumerable<IStatementLeaf> statements)
        {
            Statements = new List<IStatementLeaf>(statements);
        }

        public void AcceptStatementVisitor(IStatementVisitor visitor)
        {
            visitor.VisitBody(this);
        }

        public void VisitStatements(IStatementVisitor visitor)
        {
            foreach (IStatementLeaf statement in Statements) {
                statement.AcceptStatementVisitor(visitor);
            }
        }
    }
}
