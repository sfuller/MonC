using System;
using System.Collections.Generic;
using System.Linq;
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
    public class TranslateIdentifiersVisitor : NoOpExpressionAndStatementVisitor,
            IParseTreeVisitor, IExpressionReplacementVisitor, IStatementReplacementVisitor
    {
        private readonly Dictionary<string, FunctionDefinitionLeaf> _functions;
        private readonly EnumManager _enums;

        private readonly IList<(string name, ISyntaxTreeLeaf leaf)> _errors;
        private readonly IDictionary<ISyntaxTreeLeaf, Symbol> _symbolMap;

        private IStatementLeaf _newStatementLeaf;
        private IExpressionLeaf _newExpressionLeaf;

        private readonly ScopeManager _scopeManager = new ScopeManager();

        public TranslateIdentifiersVisitor(
            Dictionary<string, FunctionDefinitionLeaf> functions,
            IList<(string name, ISyntaxTreeLeaf leaf)> errors,
            EnumManager enums,
            IDictionary<ISyntaxTreeLeaf, Symbol> symbolMap)
        {
            _functions = functions;
            _enums = enums;
            _errors = errors;
            _symbolMap = symbolMap;

            _newExpressionLeaf = new VoidExpression();
            _newStatementLeaf = new ExpressionStatementLeaf(new VoidExpression());
        }

        public void PrepareToVisit()
        {
            ShouldReplace = false;
        }

        public bool ShouldReplace { get; private set; }

        IExpressionLeaf IExpressionReplacementVisitor.NewLeaf => _newExpressionLeaf;
        IStatementLeaf IStatementReplacementVisitor.NewLeaf => _newStatementLeaf;

        public void Process(FunctionDefinitionLeaf function)
        {
            _scopeManager.ProcessFunction(function);
            IExpressionReplacementVisitor expressionReplacementVisitor = new ScopedExpressionReplacementVisitor(this, _scopeManager);
            ProcessStatementReplacementsVisitor statementReplacementsVisitor = new ProcessStatementReplacementsVisitor(this, expressionReplacementVisitor);
            ProcessExpressionReplacementsVisitor expressionReplacementsVisitor = new ProcessExpressionReplacementsVisitor(expressionReplacementVisitor);
            ExpressionChildrenVisitor expressionChildrenVisitor = new ExpressionChildrenVisitor(expressionReplacementsVisitor);
            StatementChildrenVisitor statementChildrenVisitor = new StatementChildrenVisitor(statementReplacementsVisitor, expressionChildrenVisitor);
            function.Body.AcceptStatements(statementChildrenVisitor);
        }

        public override void VisitUnknown(IExpressionLeaf leaf)
        {
            if (leaf is IParseLeaf parseLeaf) {
                parseLeaf.AcceptParseTreeVisitor(this);
            }
        }

        public void VisitAssignment(AssignmentParseLeaf leaf)
        {
        }

        public void VisitIdentifier(IdentifierParseLeaf leaf)
        {
            ShouldReplace = true;

            DeclarationLeaf decl = _scopeManager.GetScope(leaf).Variables.Find(d => d.Name == leaf.Name);
            if (decl != null) {
                _newExpressionLeaf = UpdateSymbolMap(new VariableLeaf(decl), leaf);
                return;
            }

            EnumLeaf? enumLeaf = _enums.GetEnumeration(leaf.Name);
            if (enumLeaf != null) {
                _newExpressionLeaf = UpdateSymbolMap(new EnumValueLeaf(enumLeaf, leaf.Name), leaf);
                return;
            }

            ShouldReplace = false;
            _errors.Add(($"Undeclared identifier {leaf.Name}", leaf));
        }

        public void VisitFunctionCall(FunctionCallParseLeaf leaf)
        {
            IdentifierParseLeaf? identifier = leaf.LHS as IdentifierParseLeaf;

            if (identifier == null) {
                _errors.Add(("LHS of function call operator is not an identifier.", leaf));
                return;
            }

            ShouldReplace = true;

            FunctionCallLeaf? resultLeaf = null;

            FunctionDefinitionLeaf function;
            if (!_functions.TryGetValue(identifier.Name, out function)) {
                _errors.Add(("Undefined function " + identifier.Name, leaf));
            } else if (function.Parameters.Length != leaf.ArgumentCount) {
                _errors.Add(($"Expected {function.Parameters.Length} argument(s), got {leaf.ArgumentCount}", leaf));
            } else {
                resultLeaf = new FunctionCallLeaf(function, leaf.GetArguments());
            }

            if (resultLeaf == null) {
                resultLeaf = MakeFakeFunctionCall(identifier, leaf);
            }

            UpdateSymbolMap(resultLeaf, leaf);
            _newExpressionLeaf = resultLeaf;
        }

        private FunctionCallLeaf MakeFakeFunctionCall(IdentifierParseLeaf identifier, FunctionCallParseLeaf call)
        {
            FunctionCallLeaf fakeFunctionCall = new FunctionCallLeaf(
                lhs: new FunctionDefinitionLeaf(
                    $"(placeholder) {identifier.Name}",
                    new TypeSpecifier("int", PointerType.NotAPointer),
                    Array.Empty<DeclarationLeaf>(),
                    new Body(Array.Empty<IStatementLeaf>()),
                    isExported: false
                ),
                arguments: Enumerable.Range(0, call.ArgumentCount).Select(call.GetArgument));

            return fakeFunctionCall;
        }

        private IExpressionLeaf UpdateSymbolMap(IExpressionLeaf leaf, IExpressionLeaf original)
        {
            Symbol originalSymbol;
            _symbolMap.TryGetValue(original, out originalSymbol);
            _symbolMap[leaf] = originalSymbol;
            return leaf;
        }
    }
}
