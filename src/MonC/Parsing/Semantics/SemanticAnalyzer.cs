using System.Collections.Generic;
using MonC.Parsing.Scoping;
using MonC.SyntaxTree;
using MonC.SyntaxTree.Util;

namespace MonC.Parsing.Semantics
{
    public class SemanticAnalyzer
    {
        private IList<ParseError> _errors;
        
        private readonly Dictionary<string, FunctionDefinitionLeaf> _functions = new Dictionary<string, FunctionDefinitionLeaf>();
        
        public void AnalyzeModule(Module module, IList<ParseError> errors)
        {
            _functions.Clear();
            _errors = errors;
            
            foreach (FunctionDefinitionLeaf function in module.Functions) {
                if (_functions.ContainsKey(function.Name)) {
                    _errors.Add(new ParseError {
                        Message = "Redefinition of function " + function.Name
                    });
                }
                _functions[function.Name] = function;
            } 
            
            foreach (FunctionDefinitionLeaf function in module.Functions) {
                AnalyzeFunction(function);
            }
        }

        private void AnalyzeFunction(FunctionDefinitionLeaf function)
        {
            ScopeCache scopes = new ScopeCache();
            ScopeResolver resolver = new ScopeResolver(scopes, Scope.New());
            function.Accept(resolver);
            
            VisitChildrenVisitor visitChildrenVisitor = new VisitChildrenVisitor();
            ProcessReplacementsVisitor replacementsVisitor = new ProcessReplacementsVisitor(scopes);
            
            VariableDeclarationAnalyzer declarationAnalyzer = new VariableDeclarationAnalyzer(scopes);
            function.Accept(visitChildrenVisitor.SetVisitors(declarationAnalyzer));

            ProcessAssignmentsVisitor assignmentsVisitor = new ProcessAssignmentsVisitor(scopes);
            function.Accept(visitChildrenVisitor.SetVisitors(replacementsVisitor.SetReplacer(assignmentsVisitor)));
            
            TranslateIdentifiersVisitor identifiersVisitor = new TranslateIdentifiersVisitor(scopes, _functions, _errors);
            function.Accept(visitChildrenVisitor.SetVisitors(replacementsVisitor.SetReplacer(identifiersVisitor)));
        }
        
    }
}