using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MonC.DotNetInterop;
using MonC.Parsing;
using MonC.SyntaxTree;

namespace MonC.Semantics
{
    public class HeaderModuleBuilder
    {
        private readonly InteropResolver _interopResolver = new InteropResolver();

        private readonly List<KeyValuePair<Symbol, FunctionDefinitionNode>> _functions =
            new List<KeyValuePair<Symbol, FunctionDefinitionNode>>();

        private readonly List<KeyValuePair<Symbol, EnumNode>> _enums = new List<KeyValuePair<Symbol, EnumNode>>();

        // Keep this around so the linker may setup bindings
        public InteropResolver InteropResolver => _interopResolver;

        public void AddModule(ParseModule module)
        {
            for (int i = 0, ilen = module.Functions.Count; i < ilen; ++i) {
                FunctionDefinitionNode function = module.Functions[i];
                if (function.IsExported) {
                    if (!module.TokenMap.TryGetValue(function, out Symbol symbol)) {
                        throw new InvalidOperationException($"function '{function.Name}' does not have a symbol");
                    }
                    _functions.Add(new KeyValuePair<Symbol, FunctionDefinitionNode>(symbol, function));
                }
            }

            for (int i = 0, ilen = module.Enums.Count; i < ilen; ++i) {
                EnumNode enumNode = module.Enums[i];
                if (enumNode.IsExported) {
                    if (!module.TokenMap.TryGetValue(enumNode, out Symbol symbol)) {
                        throw new InvalidOperationException($"enum '{enumNode.Name}' does not have a symbol");
                    }
                    _enums.Add(new KeyValuePair<Symbol, EnumNode>(symbol, enumNode));
                }
            }
        }

        public void AddDotNetAssembly(Assembly lib)
        {
            _interopResolver.ImportAssembly(lib, BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.Static);
        }

        public ParseModule CreateHeaderModule(IList<ParseError> errors)
        {
            // Initialize module with .net interop members
            ParseModule module = _interopResolver.CreateHeaderModule();

            Dictionary<string, Symbol> duplicateFunctionTracker =
                new Dictionary<string, Symbol>(module.Functions.Count + _functions.Count);

            // Initialize function tracker with .net members
            for (int i = 0, ilen = module.Functions.Count; i < ilen; ++i) {
                FunctionDefinitionNode function = module.Functions[i];
                if (!module.TokenMap.TryGetValue(function, out Symbol symbol)) {
                    throw new InvalidOperationException($".net function '{function.Name}' does not have a symbol");
                }
                duplicateFunctionTracker.Add(function.Name, symbol);
            }

            // Merge undeclared functions from modules
            for (int i = 0, ilen = _functions.Count; i < ilen; ++i) {
                var function = _functions[i];
                if (duplicateFunctionTracker.TryGetValue(function.Value.Name, out Symbol existingSymbol)) {
                    ParseError error;
                    error.Message = $"{function.Value.Name} previously declared at {existingSymbol.SourceFile}:{existingSymbol.Start.ToString()}";
                    error.Start = function.Key.Start;
                    error.End = function.Key.End;
                    errors.Add(error);
                } else {
                    duplicateFunctionTracker.Add(function.Value.Name, function.Key);
                    module.Functions.Add(function.Value);
                    module.TokenMap.Add(function.Value, function.Key);
                }
            }

            Dictionary<string, Symbol> duplicateEnumTracker =
                new Dictionary<string, Symbol>(module.Enums.Sum(x => x.Enumerations.Length) +
                                               _enums.Sum(x => x.Value.Enumerations.Length));

            // Initialize enum tracker with .net members
            for (int i = 0, ilen = module.Enums.Count; i < ilen; ++i) {
                EnumNode enumNode = module.Enums[i];
                if (!module.TokenMap.TryGetValue(enumNode, out Symbol symbol)) {
                    throw new InvalidOperationException($".net enum '{enumNode.Name}' does not have a symbol");
                }
                for (int j = 0, jlen = enumNode.Enumerations.Length; j < jlen; ++j) {
                    var enumeration = enumNode.Enumerations[j];
                    duplicateEnumTracker.Add(enumeration.Key, symbol);
                }
            }

            // Merge undeclared enums from modules
            for (int i = 0, ilen = _enums.Count; i < ilen; ++i) {
                var enumEntry = _enums[i];
                bool good = true;
                for (int j = 0, jlen = enumEntry.Value.Enumerations.Length; j < jlen; ++j) {
                    var enumeration = enumEntry.Value.Enumerations[j];
                    if (duplicateEnumTracker.TryGetValue(enumeration.Key, out Symbol existingSymbol)) {
                        ParseError error;
                        error.Message =
                            $"enumeration {enumeration.Key} previously declared at {existingSymbol.SourceFile}:{existingSymbol.Start.ToString()}";
                        error.Start = enumEntry.Key.Start;
                        error.End = enumEntry.Key.End;
                        errors.Add(error);
                        good = false;
                    } else {
                        duplicateEnumTracker.Add(enumeration.Key, enumEntry.Key);
                    }
                }
                if (good) {
                    module.Enums.Add(enumEntry.Value);
                    module.TokenMap.Add(enumEntry.Value, enumEntry.Key);
                }
            }

            return module;
        }
    }
}
