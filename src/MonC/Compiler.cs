using System.Collections.Generic;
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
            Optional<ParseModule> targetModule = default(Optional<ParseModule>)
        )
        {
            Lexer lexer = new Lexer();
            List<Token> tokens = new List<Token>();
            lexer.Lex(source, tokens);
            
            Parser parser = new Parser();

            ParseModule module;
            if (!targetModule.Get(out module)) {
                module = new ParseModule();
            }
            
            parser.Parse(filename, tokens, module, errors);
            
            if (errors.Count > 0) {
                return new Optional<ParseModule>();
            }
            return new Optional<ParseModule>(module);
        }

        public Optional<ParseModule> Parse(
            string source,
            string filename,
            List<ParseError> errors,
            IEnumerable<Binding> bindings
        )
        {
            ParseModule module = new ParseModule();
            foreach (Binding binding in bindings) {
                module.Functions.Add(binding.Prototype);
            }
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
            IEnumerable<Binding> bindings,
            List<ParseError> parseErrors,
            List<LinkError> linkErrors
        )
        {
            Linker linker = new Linker();
            
            ParseModule parsedModule = new ParseModule();
            
            foreach (Binding binding in bindings) {
                parsedModule.Functions.Add(binding.Prototype);
                linker.AddFunctionBinding(binding.Prototype.Name, binding.Implementation);
            }

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
            IEnumerable<Binding> bindings,
            List<LinkError> linkErrors
        )
        {
            Linker linker = new Linker();
            
            foreach (Binding binding in bindings) {
                linker.AddFunctionBinding(binding.Prototype.Name, binding.Implementation);
            }

            ILModule compiledModule = Compile(parsedModule);

            linker.AddModule(compiledModule);

            VMModule linkedModule = linker.Link(linkErrors);
            if (linkErrors.Count > 0) {
                return new Optional<VMModule>();
            }

            return new Optional<VMModule>(linkedModule);
        }

    }
}