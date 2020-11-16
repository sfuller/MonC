using System.Collections.Generic;

namespace MonC.SyntaxTree.Nodes.Statements
{
    public class BodyNode : StatementNode
    {
        public readonly BodyType Type;
        public readonly List<IStatementNode> Statements;

        public BodyNode()
        {
            Statements = new List<IStatementNode>();
        }

        public BodyNode(BodyType type, IEnumerable<IStatementNode> statements)
        {
            Type = type;
            Statements = new List<IStatementNode>(statements);
        }

        public override void AcceptStatementVisitor(IStatementVisitor visitor)
        {
            visitor.VisitBody(this);
        }

        public void VisitStatements(IStatementVisitor visitor)
        {
            foreach (IStatementNode statement in Statements) {
                statement.AcceptStatementVisitor(visitor);
            }
        }
    }
}
