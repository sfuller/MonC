using System;
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

        private Job _job;
        private IParseInput _parseInput;
        private ParseModule _parseModule;
        private bool _ranSemanticAnalysis;

        public ParseTool(Job job, IParseInput parseInput)
        {
            _job = job;
            _parseInput = parseInput;
        }

        public static ParseTool Construct(Job job, IParseInput input) => new ParseTool(job, input);

        public void RunHeaderPass()
        {
            if (_parseModule != null)
                throw new InvalidOperationException("RunHeaderPass has already been called");

            Parser parser = new Parser();
            List<ParseError> errors = new List<ParseError>();
            _parseModule = parser.Parse(_parseInput.GetFileInfo().FullPath, _parseInput.GetTokens(), errors);

            for (int i = 0, ilen = errors.Count; i < ilen; ++i) {
                ParseError error = errors[i];
                Diagnostics.Report(Diagnostics.Severity.Error,
                    $"{error.Start.Line + 1},{error.Start.Column + 1}: {error.Message}");
            }

            if (_showAST) {
                PrintTreeVisitor treeVisitor = new PrintTreeVisitor(Console.Out);
                for (int i = 0, ilen = _parseModule.Functions.Count; i < ilen; ++i) {
                    _parseModule.Functions[i].AcceptTopLevelVisitor(treeVisitor);
                }
            }

            if (!_job._forceCodegen)
                Diagnostics.ThrowIfErrors();

            // Register module members with semantic analyzer
            _job._semanticAnalyzer.RegisterModule(_parseModule);
        }

        public ParseModule GetParseModule()
        {
            if (_parseModule == null)
                throw new InvalidOperationException("RunHeaderPass has not been called");

            if (!_ranSemanticAnalysis) {
                _job._semanticAnalyzer.Process(_parseModule);
                _ranSemanticAnalysis = true;
            }

            return _parseModule;
        }

        public FileInfo GetFileInfo() => _parseInput.GetFileInfo();

        public void WriteInputChain(TextWriter writer)
        {
            _parseInput.WriteInputChain(writer);
            writer.WriteLine("  -ParseTool");
        }
    }
}
