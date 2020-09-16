using System.Collections.Generic;
using MonC.Parsing.ParseTreeLeaves;
using MonC.Parsing.Scoping;
using MonC.SyntaxTree;
using MonC.SyntaxTree.Leaves;
using MonC.SyntaxTree.Leaves.Expressions;
using MonC.SyntaxTree.Leaves.Statements;
using MonC.SyntaxTree.Util.ChildrenVisitors;
using MonC.SyntaxTree.Util.NoOpVisitors;
using MonC.SyntaxTree.Util.ReplacementVisitors;

namespace MonC.Parsing.Semantics
{
    public class AssignmentAnalyzer :
            NoOpExpressionAndStatementVisitor, IParseTreeVisitor,
            IStatementReplacementVisitor, IExpressionReplacementVisitor
    {
        private readonly IList<(string message, ISyntaxTreeLeaf leaf)> _errors;
        private readonly IDictionary<ISyntaxTreeLeaf, Symbol> _symbolMap;

        private bool _shouldReplace;
        private IExpressionLeaf _newExpressionLeaf;

        private readonly ScopeManager _scopeManager = new ScopeManager();

        // We don't reaplace any statement leaves, so we can keep this a constant, non-null value.
        private readonly IStatementLeaf _newStatementLeaf = new ExpressionStatementLeaf(new VoidExpression());

        public AssignmentAnalyzer(IList<(string message, ISyntaxTreeLeaf leaf)> errors, IDictionary<ISyntaxTreeLeaf, Symbol> symbolMap)
        {
            _errors = errors;
            _symbolMap = symbolMap;

            _newExpressionLeaf = new VoidExpression();
        }

        public void Process(FunctionDefinitionLeaf function)
        {
            _scopeManager.ProcessFunction(function);

            ProcessExpressionReplacementsVisitor expressionReplacementsVisitor = new ProcessExpressionReplacementsVisitor(this);
            ProcessStatementReplacementsVisitor statementReplacementsVisitor = new ProcessStatementReplacementsVisitor(this, this);
            ExpressionChildrenVisitor expressionChildrenVisitor = new ExpressionChildrenVisitor(expressionReplacementsVisitor);
            StatementChildrenVisitor statementChildrenVisitor = new StatementChildrenVisitor(statementReplacementsVisitor, expressionChildrenVisitor);
            function.Body.VisitStatements(statementChildrenVisitor);
        }

        public void PrepareToVisit()
        {
            _shouldReplace = false;
        }

        bool IReplacementVisitor<IExpressionLeaf>.ShouldReplace => _shouldReplace;
        bool IReplacementVisitor<IStatementLeaf>.ShouldReplace => _shouldReplace;
        IExpressionLeaf IReplacementVisitor<IExpressionLeaf>.NewLeaf => _newExpressionLeaf;
        IStatementLeaf IReplacementVisitor<IStatementLeaf>.NewLeaf => _newStatementLeaf;

        public override void VisitBinaryOperation(IBinaryOperationLeaf leaf)
        {
            leaf.AcceptBinaryOperationVisitor(this);
        }

        public override void VisitUnknown(IExpressionLeaf leaf)
        {
            if (leaf is IParseLeaf parseLeaf) {
                parseLeaf.AcceptParseTreeVisitor(this);
            }
        }

        public override void VisitUnknown(IBinaryOperationLeaf leaf)
        {
            if (leaf is IParseLeaf parseLeaf) {
                parseLeaf.AcceptParseTreeVisitor(this);
            }
        }

        public void VisitAssignment(AssignmentParseLeaf leaf)
        {
            if (!(leaf.LHS is IdentifierParseLeaf identifier)) {
                _errors.Add(("Expecting identifier", leaf.LHS));
                return;
            }

            _shouldReplace = true;
            IExpressionLeaf resultLeaf;

            DeclarationLeaf declaration = _scopeManager.GetScope(leaf).Variables.Find(d => d.Name == identifier.Name);
            if (declaration == null) {
                _errors.Add(($"Undeclared identifier {identifier.Name}", identifier));
                resultLeaf = new VoidExpression();
            } else {
                resultLeaf = new AssignmentLeaf(declaration, leaf.RHS);
            }

            _newExpressionLeaf = resultLeaf;

            // TODO: Need more automated symbol association for new leaves.
            Symbol originalSymbol;
            _symbolMap.TryGetValue(leaf, out originalSymbol);
            _symbolMap[_newExpressionLeaf] = originalSymbol;
        }

        public void VisitIdentifier(IdentifierParseLeaf leaf)
        {
        }

        public void VisitFunctionCall(FunctionCallParseLeaf leaf)
        {
        }
    }
}
