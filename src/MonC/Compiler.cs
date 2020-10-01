using System;
using System.Collections.Generic;
using System.Linq;
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
        public ParseModule ParseAndAnalyze(
            string source,
            string filename,
            List<ParseError> errors,
            ParseModule? headerModule = null
        )
        {
            Lexer lexer = new Lexer();
            List<Token> tokens = new List<Token>();
            lexer.LexFullModule(source, tokens);

            Parser parser = new Parser();
            ParseModule outputModule = parser.Parse(filename, tokens, errors);
            SemanticAnalyzer analyzer = new SemanticAnalyzer(errors);

            if (headerModule != null) {
                analyzer.Register(headerModule);
            }
            analyzer.Register(outputModule);
            analyzer.Process(outputModule);
            return outputModule;
        }

        public ParseModule ParseAndAnalyze(
            string source,
            string filename,
            List<ParseError> errors,
            InteropResolver resolver
        )
        {
            ParseModule module = CreateInputParseModuleFromInteropResolver(resolver);
            return ParseAndAnalyze(source, filename, errors, module);
        }

        public ILModule? Compile(
            string source,
            string filename,
            List<ParseError> errors,
            ParseModule? targetModule = null
        )
        {
            ParseModule module = ParseAndAnalyze(source, filename, errors, targetModule);
            if (errors.Count > 0) {
                return null;
            }
            return Compile(module);
        }

        public ILModule Compile(ParseModule module)
        {
            throw new NotImplementedException();
            // CodeGenerator generator = new CodeGenerator(module, context);
            // return generator.Generate();
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
