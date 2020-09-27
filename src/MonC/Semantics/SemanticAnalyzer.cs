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

        private readonly Dictionary<string, FunctionDefinitionNode> _functions = new Dictionary<string, FunctionDefinitionNode>();
        private readonly EnumManager _enumManager;
        private readonly TypeManager _typeManager;

        /// <summary>
        /// Map to the symbol for each syntax tree node in the analyzed modules and loaded header modules.
        /// </summary>
        private readonly Dictionary<ISyntaxTreeNode, Symbol> _symbolMap = new Dictionary<ISyntaxTreeNode, Symbol>();

        /// <summary>
        /// Error information for the current module being analyzed. This information is processed and cleared at the
        /// end of <see cref="Process"/>.
        /// </summary>
        private readonly List<(string message, ISyntaxTreeNode node)> _errorsToProcess = new List<(string message, ISyntaxTreeNode node)>();

        /// <summary>
        /// Constructs a SemanticAnalyzer which uses a supplied list to store errors.
        /// </summary>
        public SemanticAnalyzer(IList<ParseError> errors)
        {
            _errors = errors;
            _enumManager = new EnumManager(errors);
            _typeManager = new TypeManager();

            _typeManager.RegisterType(new PrimitiveTypeImpl("int"));
        }

        public void LoadHeaderModule(ParseModule headerModule)
        {
            RegisterModuleContents(headerModule);
        }

        /// <summary>
        /// <para>Analyzes the given  and transforms the parse module's tree from a 'parse' tree into a final syntax
        /// tree.
        /// </para>
        /// <para>Note on 'parse' tree: MonC's parser doesn't produce a formal parse tree as it doesn't perserve
        /// non-significant information. We use the term 'parse tree' to signify that the tree comes straight from the
        /// parser and isn't analyzed and annotated.</para>
        /// </summary>
        public void Process(ParseModule module)
        {
            RegisterModuleContents(module);

            foreach (FunctionDefinitionNode function in module.Functions) {
                ProcessFunction(function);
            }

            foreach ((string message, ISyntaxTreeNode node) in _errorsToProcess) {
                Symbol symbol;
                _symbolMap.TryGetValue(node, out symbol);
                _errors.Add(new ParseError {Message = message, Start = symbol.Start, End = symbol.End});
            }
            _errorsToProcess.Clear();
        }

        private void AddSymbols(Dictionary<ISyntaxTreeNode, Symbol> symbolMap)
        {
            foreach (KeyValuePair<ISyntaxTreeNode, Symbol> symbolMapping in symbolMap) {
                _symbolMap.Add(symbolMapping.Key, symbolMapping.Value);
            }
        }

        private void RegisterModuleContents(ParseModule module)
        {
            AddSymbols(module.SymbolMap);
            RegisterFunctions(module);
            RegisterEnums(module);
        }

        private void RegisterFunctions(ParseModule module)
        {
            foreach (FunctionDefinitionNode function in module.Functions) {
                if (_functions.ContainsKey(function.Name)) {
                    // TODO: More information in error.
                    _errors.Add(new ParseError {Message = "Duplicate function"});
                } else {
                    _functions.Add(function.Name, function);
                }
            }
        }

        private void RegisterEnums(ParseModule module)
        {
            foreach (EnumNode enumNode in module.Enums) {
                _enumManager.RegisterEnum(enumNode);
                _typeManager.RegisterType(new PrimitiveTypeImpl(enumNode.Name));
            }
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
