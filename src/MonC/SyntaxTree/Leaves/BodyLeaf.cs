using System;
using System.Collections.Generic;

namespace MonC.SyntaxTree
{
    public class BodyLeaf : IASTLeaf
    {
        private readonly List<IASTLeaf> _statements;

        public int Length {
            get { return _statements.Count; }
        }
        
        public BodyLeaf(IEnumerable<IASTLeaf> statements)
        {
            _statements = new List<IASTLeaf>(statements);
        }
        
        public void Accept(IASTLeafVisitor visitor)
        {
            visitor.VisitBody(this);
        }

        public void ReplaceAllStatements(Func<IASTLeaf, IASTLeaf> replacer)
        {
            for (int i = 0, ilen = _statements.Count; i < ilen; ++i)
                _statements[i] = replacer(_statements[i]);
        }

        public IASTLeaf GetStatement(int index)
        {
            return _statements[index];
        }

        public void SetStatement(int index, IASTLeaf statement)
        {
            _statements[index] = statement;
        }

        public void AddStatement(IASTLeaf statement)
        {
            _statements.Add(statement);
        }

        public void InsertStatement(int index, IASTLeaf statement)
        {
            _statements.Insert(index, statement);
        }
        
        public void RemoveStatement(int index)
        {
            _statements.RemoveAt(index);
        }
           
    }
}