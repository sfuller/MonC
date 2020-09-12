using System.Collections.Generic;
using MonC.Parsing.Semantics.TypeAnalysis;
using MonC.SyntaxTree;

namespace MonC.Parsing.Semantics
{
    public class SemanticAnalyzer
    {
        private readonly IList<ParseError> _errors;
        private readonly IDictionary<ISyntaxTreeLeaf, Symbol> _symbolMap;
        private readonly EnumManager _enumManager;

        private readonly Dictionary<string, FunctionDefinitionLeaf> _functions = new Dictionary<string, FunctionDefinitionLeaf>();

        private readonly List<(string message, ISyntaxTreeLeaf leaf)> _errorsToProcess = new List<(string message, ISyntaxTreeLeaf leaf)>();

        public SemanticAnalyzer(IList<ParseError> errors, IDictionary<ISyntaxTreeLeaf, Symbol> symbolMap)
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
                ProcessFunction(function);
            }

            foreach ((string message, ISyntaxTreeLeaf leaf) in _errorsToProcess) {
                Symbol symbol;
                _symbolMap.TryGetValue(leaf, out symbol);
                _errors.Add(new ParseError {Message = message, Start = symbol.Start, End = symbol.End});
            }
        }

        private void ProcessFunction(FunctionDefinitionLeaf function)
        {
            new VariableDeclarationProcessor(_errorsToProcess).Process(function);
            new AssignmentAnalyzer(_errorsToProcess, _symbolMap).Process(function);
            new TranslateIdentifiersVisitor(_functions, _errorsToProcess, _enumManager, _symbolMap).Process(function);
            new TypeCheckVisitor(_errorsToProcess).Process(function);
        }

    }
}
