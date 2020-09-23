using System.Collections.Generic;
using MonC.Parsing;
using MonC.Semantics.TypeChecks;
using MonC.TypeSystem;
using MonC.SyntaxTree;
using MonC.TypeSystem.Types.Impl;

namespace MonC.Semantics
{
    public class SemanticAnalyzer : IErrorManager
    {
        private readonly IList<ParseError> _errors;
        private readonly IDictionary<ISyntaxTreeNode, Symbol> _symbolMap;
        private readonly EnumManager _enumManager;

        private readonly Dictionary<string, FunctionDefinitionNode> _functions = new Dictionary<string, FunctionDefinitionNode>();

        private readonly List<(string message, ISyntaxTreeNode node)> _errorsToProcess = new List<(string message, ISyntaxTreeNode node)>();

        private readonly TypeManager _typeManager;

        public SemanticAnalyzer(IList<ParseError> errors, IDictionary<ISyntaxTreeNode, Symbol> symbolMap)
        {
            _errors = errors;
            _symbolMap = symbolMap;
            _enumManager = new EnumManager(errors);

            _typeManager = new TypeManager();
        }

        public void Analyze(ParseModule headerModule, ParseModule newModule)
        {
            _functions.Clear();

            // TODO: Load types from header module.

            _typeManager.RegisterType(new PrimitiveTypeImpl("int"));


            foreach (EnumNode enumNode in headerModule.Enums) {
                RegisterEnum(enumNode);
            }
            foreach (EnumNode enumNode in newModule.Enums) {
                RegisterEnum(enumNode);
            }

            foreach (FunctionDefinitionNode externalFunction in headerModule.Functions) {
                _functions.Add(externalFunction.Name, externalFunction);
            }
            foreach (FunctionDefinitionNode function in newModule.Functions) {
                if (_functions.ContainsKey(function.Name)) {
                    _errorsToProcess.Add(("Redefinition of function " + function.Name, function));
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

        private void RegisterEnum(EnumNode enumNode)
        {
            _enumManager.RegisterEnum(enumNode);
            _typeManager.RegisterType(new PrimitiveTypeImpl(enumNode.Name));
        }

        private void ProcessFunction(FunctionDefinitionNode function)
        {
            new DuplicateVariableDeclarationAnalyzer(this).Process(function);
            new AssignmentAnalyzer(this, _symbolMap).Process(function);
            new TranslateIdentifiersVisitor(_functions, this, _enumManager, _symbolMap).Process(function);
            new TypeResolver(_typeManager, this).Process(function);
            new TypeCheckVisitor(_typeManager, this).Process(function);
        }

        void IErrorManager.AddError(string message, ISyntaxTreeNode node)
        {
            _errorsToProcess.Add((message, node));
        }
    }
}
