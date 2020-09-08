using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MonC.Bytecode;

namespace MonC.Codegen
{
    public class ILListingWriter
    {
        private readonly TextWriter _writer;
        private readonly Dictionary<string, string[]> _files = new Dictionary<string, string[]>();

        public ILListingWriter(TextWriter writer)
        {
            _writer = writer;
        }
        
        public void Write(ILModule module)
        {
            Dictionary<int, string> exportedFunctionNames = module.ExportedFunctions.ToDictionary(x => x.Value, x => x.Key);
            
            for (int i = 0, ilen = module.DefinedFunctions.Length; i < ilen; ++i) {
                string? name;
                if (!exportedFunctionNames.TryGetValue(i, out name)) {
                    name = "";
                }
                WriteFunction(name, i, module.DefinedFunctions[i]);
            }
        }

        private void WriteFunction(string name, int index, ILFunction function)
        {
            _writer.WriteLine($"[{index}] {name}");

            var code = function.Code;
            var symbols = function.Symbols;
            
            for (int i = 0, ilen = code.Length; i < ilen; ++i) {
                Instruction instruction = code[i];
                
                _writer.Write($"  {i,8}:  {instruction.Op} \t{instruction.ImmediateValue}");
                
                Symbol symbol;
                if (symbols.TryGetValue(i, out symbol)) {
                    _writer.Write($"\t; {GetSnippet(symbol)}");
                }
                
                _writer.WriteLine();
            }
            _writer.WriteLine();
        }

        private string GetSnippet(Symbol symbol)
        {
            string[]? file = GetFile(symbol.SourceFile);

            if (file == null) {
                return GetDefaultSnippet(symbol);
            }

            if (symbol.Start.Line >= file.Length) {
                return GetDefaultSnippet(symbol);
            }

            string line = file[symbol.Start.Line];

            uint colStart = symbol.Start.Column;
            uint colEnd = symbol.End.Column;
            
            if (symbol.End.Line != symbol.Start.Line) {
                colEnd = (uint)line.Length - 1;
            }

            if (colEnd > line.Length) {
                return GetDefaultSnippet(symbol);
            }

            return line.Substring((int)colStart, (int)(colEnd - colStart));
        }

        private string GetDefaultSnippet(Symbol symbol)
        {
            return $"<{symbol.SourceFile}; {symbol.Start.Line},{symbol.Start.Column} : {symbol.End.Line},{symbol.End.Column}>";
        }

        private string[]? GetFile(string? path)
        {
            if (path == null) {
                return null;
            }
            
            string[]? file;
            if (_files.TryGetValue(path, out file)) {
                return file;
            }
            
            try {
                file = File.ReadAllLines(path);
            } catch (Exception) {
                return null;
            }
            
            _files[path] = file;
            return file;
        }
    }
}