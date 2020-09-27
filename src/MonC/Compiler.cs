using System.Collections.Generic;
using System.Linq;
using MonC.Codegen;
using MonC.DotNetInterop;
using MonC.IL;
using MonC.Parsing;
using MonC.Semantics;
using MonC.VM;

namespace MonC
{
    /// <summary>
    /// A basic MonC compiler implementation. Note that the example frontend does not use this class, as it uses data
    /// from each component for example purposes, something that is not needed for most integrations of MonC.
    /// </summary>
    public class Compiler
    {
        public ParseModule? Parse(
            string source,
            string filename,
            List<ParseError> errors,
            ParseModule? headerModule = null
        )
        {
            Lexer lexer = new Lexer();
            List<Token> tokens = new List<Token>();
            lexer.LexFullModule(source, tokens);

            if (headerModule == null) {
                headerModule = new ParseModule();
            }

            Parser parser = new Parser();
            ParseModule outputModule = parser.Parse(filename, tokens, errors);
            SemanticAnalyzer.AnalyzeModule(outputModule, headerModule, errors);

            if (errors.Count > 0) {
                return null;
            }
            return outputModule;
        }

        public ParseModule? Parse(
            string source,
            string filename,
            List<ParseError> errors,
            InteropResolver resolver
        )
        {
            ParseModule module = CreateInputParseModuleFromInteropResolver(resolver);
            return Parse(source, filename, errors, module);
        }

        public ILModule? Compile(
            string source,
            string filename,
            List<ParseError> errors,
            ParseModule? targetModule = null
        )
        {
            ParseModule? parsedModule = Parse(source, filename, errors, targetModule);
            if (parsedModule == null) {
                return null;
            }
            return Compile(parsedModule);
        }

        public ILModule Compile(ParseModule module)
        {
            CodeGenerator generator = new CodeGenerator();
            return generator.Generate(module);
        }

        public VMModule? CompileAndLink(
            string source,
            string filename,
            InteropResolver resolver,
            List<ParseError> parseErrors,
            List<LinkError> linkErrors
        )
        {
            Linker linker = new Linker(linkErrors);
            ParseModule parsedModule = CreateInputParseModuleFromInteropResolver(resolver);

            SetupLinkerWithInteropResolver(linker, resolver);

            ILModule? compiledModule = Compile(source, filename, parseErrors, parsedModule);
            if (compiledModule == null) {
                return null;
            }

            linker.AddModule(compiledModule, export: true);

            VMModule linkedModule = linker.Link();
            if (linkErrors.Count > 0) {
                return null;
            }

            return linkedModule;
        }

        public VMModule? CompileAndLink(
            ParseModule parsedModule,
            InteropResolver resolver,
            List<LinkError> linkErrors
        )
        {
            Linker linker = new Linker(linkErrors);
            SetupLinkerWithInteropResolver(linker, resolver);
            ILModule compiledModule = Compile(parsedModule);

            linker.AddModule(compiledModule, export: true);

            VMModule linkedModule = linker.Link();
            if (linkErrors.Count > 0) {
                return null;
            }

            return linkedModule;
        }

        private ParseModule CreateInputParseModuleFromInteropResolver(InteropResolver resolver)
        {
            ParseModule module = new ParseModule();
            module.Functions.AddRange(resolver.Bindings.Select(b => b.Prototype));
            module.Enums.AddRange(resolver.Enums);
            return module;
        }

        private void SetupLinkerWithInteropResolver(Linker linker, InteropResolver resolver)
        {
            foreach (Binding binding in resolver.Bindings) {
                linker.AddFunctionBinding(binding.Prototype.Name, binding.Implementation, export: false);
            }
        }

    }
}
