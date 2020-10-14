using System.Collections.Generic;
using MonC.Parsing;
using MonC.Semantics.TypeChecks;
using MonC.TypeSystem;
using MonC.SyntaxTree;
using MonC.SyntaxTree.Nodes;
using MonC.SyntaxTree.Nodes.Expressions;
using MonC.TypeSystem.Types;
using MonC.TypeSystem.Types.Impl;

namespace MonC.Semantics
{
    public class SemanticAnalyzer : IErrorManager
    {
        private readonly IList<ParseError> _errors;

        private readonly SemanticContext _context = new SemanticContext();

        private readonly TypeManager _typeManager;

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
            _typeManager = new TypeManager();

            _typeManager.RegisterType(new PrimitiveTypeImpl("void"));
            _typeManager.RegisterType(new PrimitiveTypeImpl("int"));
        }

        public SemanticContext Context => _context;

        public void Register(ParseModule module)
        {
            RegisterModule(_context, module);
        }

        /// <summary>
        /// <para>Analyzes and transforms the parse module's tree from a 'parse' tree into a final syntax
        /// tree. Modules must be registered before they are processed.
        /// </para>
        /// <para>Note on 'parse' tree: MonC's parser doesn't produce a formal parse tree as it doesn't perserve
        /// non-significant information. We use the term 'parse tree' to signify that the tree comes straight from the
        /// parser and isn't analyzed and annotated.</para>
        /// </summary>
        public SemanticModule Process(ParseModule module)
        {
            Dictionary<IExpressionNode, IType> expressionResultTypes = new Dictionary<IExpressionNode, IType>();
            ExpressionTypeManager expressionTypeManager
                = new ExpressionTypeManager(_context, _typeManager, this, expressionResultTypes);

            foreach (EnumNode enumNode in module.Enums) {
                AnalyzeEnum(enumNode);
            }

            foreach (StructNode structNode in module.Structs) {
                AnalyzeStruct(structNode);
            }

            foreach (FunctionDefinitionNode function in module.Functions) {
                AnalyzeFunction(function, expressionTypeManager);
            }

            foreach ((string message, ISyntaxTreeNode node) in _errorsToProcess) {
                Symbol symbol;
                _context.SymbolMap.TryGetValue(node, out symbol);
                _errors.Add(new ParseError {Message = message, Start = symbol.Start, End = symbol.End});
            }
            _errorsToProcess.Clear();

            return new SemanticModule(module, expressionResultTypes);
        }

        private void RegisterModule(SemanticContext context, ParseModule module)
        {
            AddSymbols(context, module.SymbolMap);
            RegisterFunctions(context, module);
            RegisterEnums(context, module);
            RegisterStructs(context, module);
        }

        private void AddSymbols(SemanticContext context, Dictionary<ISyntaxTreeNode, Symbol> symbolMap)
        {
            foreach (KeyValuePair<ISyntaxTreeNode, Symbol> symbolMapping in symbolMap) {
                context.SymbolMap.Add(symbolMapping.Key, symbolMapping.Value);
            }
        }

        private void RegisterFunctions(SemanticContext context, ParseModule module)
        {
            foreach (FunctionDefinitionNode function in module.Functions) {
                if (context.Functions.ContainsKey(function.Name)) {
                    // TODO: More information in error.
                    _errors.Add(new ParseError {Message = "Duplicate function"});
                } else {
                    context.Functions.Add(function.Name, function);
                }
            }
        }

        private void RegisterEnums(SemanticContext context, ParseModule module)
        {
            foreach (EnumNode enumNode in module.Enums) {
                foreach (EnumDeclarationNode declarationNode in enumNode.Declarations) {
                    if (context.EnumInfo.ContainsKey(declarationNode.Name)) {
                        // TODO: More information in error.
                        _errors.Add(new ParseError {Message = "Duplicate enum"});
                    } else {
                        context.EnumInfo.Add(declarationNode.Name, new EnumDeclarationInfo {
                            Enum = enumNode,
                            Declaration = declarationNode
                        });
                        // TODO: Add error if there is a conflicting type.
                        _typeManager.RegisterType(new PrimitiveTypeImpl(enumNode.Name));
                    }
                }
            }
        }

        private void RegisterStructs(SemanticContext context, ParseModule module)
        {
            foreach (StructNode structNode in module.Structs) {
                if (context.Structs.ContainsKey(structNode.Name)) {
                    // TODO: More information in error.
                    _errors.Add(new ParseError {Message = "Duplicate struct"});
                } else {
                    context.Structs.Add(structNode.Name, structNode);
                    // TODO: Add error if there is a conflicting type.
                    _typeManager.RegisterType(new StructType(structNode));
                }
            }
        }

        private void AnalyzeEnum(EnumNode enumNode)
        {
            int value = 0;
            foreach (EnumDeclarationNode declaration in enumNode.Declarations) {
                EnumDeclarationInfo declarationInfo = _context.EnumInfo[declaration.Name];
                declarationInfo.Value = value++;
                _context.EnumInfo[declaration.Name] = declarationInfo;
            }
        }

        private void AnalyzeStruct(StructNode structNode)
        {
            new TypeSpecifierResolver(_typeManager, this).Process(structNode);
        }

        private void AnalyzeFunction(FunctionDefinitionNode function, ExpressionTypeManager expressionTypeManager)
        {
            new DuplicateVariableDeclarationAnalyzer(this).Process(function);
            new TypeSpecifierResolver(_typeManager, this).Process(function);
            new TranslateIdentifiersVisitor(_context, this).Process(function);
            new TranslateAccessVisitor(this, expressionTypeManager).Process(function);
            new AssignmentAnalyzer(this, _context).Process(function);
            new TypeCheckVisitor(_context, _typeManager, this, expressionTypeManager).Process(function);
        }

        void IErrorManager.AddError(string message, ISyntaxTreeNode node)
        {
            _errorsToProcess.Add((message, node));
        }
    }
}
