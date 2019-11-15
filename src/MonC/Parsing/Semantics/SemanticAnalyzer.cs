using System.Collections.Generic;
using MonC.Parsing.Scoping;
using MonC.SyntaxTree;
using MonC.SyntaxTree.Util;

namespace MonC.Parsing.Semantics
{
    public class SemanticAnalyzer
    {
        private readonly IList<ParseError> _errors;
        private readonly IDictionary<IASTLeaf, Symbol> _symbolMap;
        private readonly EnumManager _enumManager;
        
        private readonly Dictionary<string, FunctionDefinitionLeaf> _functions = new Dictionary<string, FunctionDefinitionLeaf>();

        private List<(string message, IASTLeaf leaf)> _errorsToProcess = new List<(string message, IASTLeaf leaf)>();

        public SemanticAnalyzer(IList<ParseError> errors, IDictionary<IASTLeaf, Symbol> symbolMap)
        {
            _errors = errors;
            _symbolMap = symbolMap;
            _enumManager = new EnumManager(errors);
        }
        
        public void Analyze(ParseModule headerModule, ParseModule newModule)
        {
            _functions.Clear();
            
            foreach (EnumLeaf enumLeaf in headerModule.Enums) {
                _enumManager.RegisterEnum(enumLeaf);
            }
            foreach (EnumLeaf enumLeaf in newModule.Enums) {
                _enumManager.RegisterEnum(enumLeaf);
            }

            foreach (FunctionDefinitionLeaf externalFunction in headerModule.Functions) {
                _functions.Add(externalFunction.Name, externalFunction);
            }
            foreach (FunctionDefinitionLeaf function in newModule.Functions) {
                if (_functions.ContainsKey(function.Name)) {
                    _errorsToProcess.Add(("Redefinition of function " + function.Name, function));
                }
                _functions[function.Name] = function;
            } 
            
            foreach (FunctionDefinitionLeaf function in newModule.Functions) {
                AnalyzeFunction(function);
            }
            
            foreach ((string message, IASTLeaf leaf) in _errorsToProcess) {
                Symbol symbol;
                _symbolMap.TryGetValue(leaf, out symbol);
                _errors.Add(new ParseError {Message = message, Start = symbol.Start, End = symbol.End});
            }
        }

        private void AnalyzeFunction(FunctionDefinitionLeaf function)
        {
            ScopeCache scopes = new ScopeCache();
            ScopeResolver resolver = new ScopeResolver(scopes, Scope.New());
            function.Accept(resolver);

            VisitChildrenVisitor visitChildrenVisitor = new VisitChildrenVisitor();

            VariableDeclarationAnalyzer declarationAnalyzer = new VariableDeclarationAnalyzer(scopes, _errorsToProcess);
            function.Accept(visitChildrenVisitor.SetVisitor(declarationAnalyzer));

            ProcessAssignmentsVisitor assignmentsVisitor = new ProcessAssignmentsVisitor(scopes, _errorsToProcess, _symbolMap);
            ProcessReplacementsVisitor replacementsVisitor = new ProcessReplacementsVisitor(assignmentsVisitor, scopes);
            function.Accept(visitChildrenVisitor.SetVisitor(replacementsVisitor));
            
            TranslateIdentifiersVisitor identifiersVisitor = new TranslateIdentifiersVisitor(scopes, _functions, _errorsToProcess, _enumManager, _symbolMap);
            function.Accept(visitChildrenVisitor.SetVisitor(replacementsVisitor.SetReplacer(identifiersVisitor)));
        }
        
    }
}