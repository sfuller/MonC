using System.Collections.Generic;
using System.IO;
using MonC;
using MonC.Frontend;
using MonC.Parsing;

namespace Driver
{
    [CommandLineCategory("Parse")]
    public sealed class ParseTool : ITool, ICodeGenInput
    {
        [CommandLine("-showast", "Show AST while processing")]
        private bool _showAST = false;

        [CommandLine("-force-codegen", "Continue job even if parsing fails")]
        private bool _forceCodegen = false;

        private Job _job;
        private IParseInput _parseInput;

        public ParseTool(Job job, IParseInput parseInput)
        {
            _job = job;
            _parseInput = parseInput;
        }

        public static ParseTool Construct(Job job, IParseInput input) => new ParseTool(job, input);

        public ParseModule GetParseModule()
        {
            Parser parser = new Parser();
            List<ParseError> errors = new List<ParseError>();
            ParseModule module = parser.Parse(_parseInput.GetFilename(), _parseInput.GetTokens(),
                _job.InteropHeaderModule, errors);

            for (int i = 0, ilen = errors.Count; i < ilen; ++i) {
                ParseError error = errors[i];
                Diagnostics.Report(Diagnostics.Severity.Error,
                    $"{error.Start.Line + 1},{error.Start.Column + 1}: {error.Message}");
            }

            if (_showAST) {
                PrintTreeVisitor treeVisitor = new PrintTreeVisitor();
                for (int i = 0, ilen = module.Functions.Count; i < ilen; ++i) {
                    module.Functions[i].Accept(treeVisitor);
                }
            }

            if (!_forceCodegen)
                Diagnostics.ThrowIfErrors();

            return module;
        }

        public void WriteInputChain(TextWriter writer)
        {
            _parseInput.WriteInputChain(writer);
            writer.WriteLine("  -ParseTool");
        }
    }
}