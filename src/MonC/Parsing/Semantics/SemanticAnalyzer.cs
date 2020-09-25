using System.Collections.Generic;
using MonC.Parsing.Semantics.TypeAnalysis;
using MonC.SyntaxTree;

namespace MonC.Parsing.Semantics
{
    public class SemanticAnalyzer
    {
        private readonly IList<ParseError> _errors;
        private readonly IDictionary<ISyntaxTreeNode, Symbol> _symbolMap;
        private readonly EnumManager _enumManager;

        private readonly Dictionary<string, FunctionDefinitionNode> _functions = new Dictionary<string, FunctionDefinitionNode>();

        private readonly List<(string message, ISyntaxTreeNode node)> _errorsToProcess = new List<(string message, ISyntaxTreeNode node)>();

        public SemanticAnalyzer(IList<ParseError> errors, IDictionary<ISyntaxTreeNode, Symbol> symbolMap)
        {
            _errors = errors;
            _symbolMap = symbolMap;
            _enumManager = new EnumManager(errors);
        }

        public void Analyze(ParseModule headerModule, ParseModule newModule)
        {
            _functions.Clear();

            foreach (EnumNode enumNode in headerModule.Enums) {
                _enumManager.RegisterEnum(enumNode);
            }
            foreach (EnumNode enumNode in newModule.Enums) {
                _enumManager.RegisterEnum(enumNode);
            }

            foreach (FunctionDefinitionNode externalFunction in headerModule.Functions) {
                _functions.Add(externalFunction.Name, externalFunction);
            }
            foreach (FunctionDefinitionNode function in newModule.Functions) {
                if (_functions.TryGetValue(function.Name, out FunctionDefinitionNode existingFunction)) {
                    if (!ReferenceEquals(function, existingFunction)) {
                        _errorsToProcess.Add(("Redefinition of function " + function.Name, function));
                    }
                }
                _functions[function.Name] = function;
            }

            foreach (FunctionDefinitionNode function in newModule.Functions) {
                ProcessFunction(function);
            }

            foreach ((string message, ISyntaxTreeNode node) in _errorsToProcess) {
                Symbol symbol;
                _symbolMap.TryGetValue(node, out symbol);
                _errors.Add(new ParseError {Message = message, Start = symbol.Start, End = symbol.End});
            }
        }

        private void ProcessFunction(FunctionDefinitionNode function)
        {
            new VariableDeclarationProcessor(_errorsToProcess).Process(function);
            new AssignmentAnalyzer(_errorsToProcess, _symbolMap).Process(function);
            new TranslateIdentifiersVisitor(_functions, _errorsToProcess, _enumManager, _symbolMap).Process(function);
            new TypeCheckVisitor(_errorsToProcess).Process(function);
        }

    }
}
