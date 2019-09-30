using System.Collections.Generic;
using System.Linq;
using MonC.Codegen;
using MonC.DotNetInterop;
using MonC.Parsing;
using MonC.VM;

namespace MonC
{
    /// <summary>
    /// A basic MonC compiler implementation. Note that the example frontend does not use this class, as it uses data
    /// from each component for example purposes, something that is not needed for most integrations of MonC. 
    /// </summary>
    public class Compiler
    {
        public Optional<ParseModule> Parse(
            string source,
            string filename,
            List<ParseError> errors,
            Optional<ParseModule> optionalHeaderModule = default(Optional<ParseModule>)
        )
        {
            Lexer lexer = new Lexer();
            List<Token> tokens = new List<Token>();
            lexer.Lex(source, tokens);

            ParseModule headerModule;
            if (!optionalHeaderModule.Get(out headerModule)) {
                headerModule = new ParseModule();
            }
            
            Parser parser = new Parser();
            ParseModule outputModule = parser.Parse(filename, tokens, headerModule, errors);
            
            if (errors.Count > 0) {
                return new Optional<ParseModule>();
            }
            return new Optional<ParseModule>(outputModule);
        }

        public Optional<ParseModule> Parse(
            string source,
            string filename,
            List<ParseError> errors,
            InteropResolver resolver
        )
        {
            ParseModule module = CreateInputParseModuleFromInteropResolver(resolver);
            return Parse(source, filename, errors, new Optional<ParseModule>(module));
        }
            
        public Optional<ILModule> Compile(
            string source,
            string filename,
            List<ParseError> errors,
            Optional<ParseModule> targetModule = default(Optional<ParseModule>)
        )
        {
            ParseModule parsedModule;
            if (!Parse(source, filename, errors, targetModule).Get(out parsedModule)) {
                return new Optional<ILModule>();
            }

            return new Optional<ILModule>(Compile(parsedModule));
        }
        
        public ILModule Compile(ParseModule module) 
        {
            CodeGenerator generator = new CodeGenerator();
            return generator.Generate(module);
        }

        public Optional<VMModule> CompileAndLink(
            string source,
            string filename,
            InteropResolver resolver,
            List<ParseError> parseErrors,
            List<LinkError> linkErrors
        )
        {
            Linker linker = new Linker();
            ParseModule parsedModule = CreateInputParseModuleFromInteropResolver(resolver);
            
            SetupLinkerWithInteropResolver(linker, resolver);

            ILModule compiledModule;
            if (!Compile(source, filename, parseErrors, new Optional<ParseModule>(parsedModule)).Get(out compiledModule)) {
                return new Optional<VMModule>();
            }
            
            linker.AddModule(compiledModule);

            VMModule linkedModule = linker.Link(linkErrors);
            if (linkErrors.Count > 0) {
                return new Optional<VMModule>();
            }

            return new Optional<VMModule>(linkedModule);
        }
        
        public Optional<VMModule> CompileAndLink(
            ParseModule parsedModule,
            InteropResolver resolver,
            List<LinkError> linkErrors
        )
        {
            Linker linker = new Linker();
            SetupLinkerWithInteropResolver(linker, resolver);
            ILModule compiledModule = Compile(parsedModule);

            linker.AddModule(compiledModule);

            VMModule linkedModule = linker.Link(linkErrors);
            if (linkErrors.Count > 0) {
                return new Optional<VMModule>();
            }

            return new Optional<VMModule>(linkedModule);
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
                linker.AddFunctionBinding(binding.Prototype.Name, binding.Implementation);
            }
        }

    }
}