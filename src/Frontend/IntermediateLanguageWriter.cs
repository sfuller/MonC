using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MonC.Bytecode;
using MonC.Codegen;

namespace MonC.Frontend
{
    public class IntermediateLanguageWriter
    {
        private TextWriter _writer;
        private readonly Dictionary<string, string[]> _files = new Dictionary<string, string[]>();
        
        public void Write(ILModule module, TextWriter writer)
        {
            _writer = writer;

            Dictionary<int, string> exportedFunctionNames = module.ExportedFunctions.ToDictionary(x => x.Value, x => x.Key);
            
            for (int i = 0, ilen = module.DefinedFunctions.Length; i < ilen; ++i) {
                string name;
                if (!exportedFunctionNames.TryGetValue(i, out name)) {
                    name = "";
                }
                WriteFunction(name, i, module.DefinedFunctions[i]);
            }
        }

        public void WriteFunction(string name, int index, ILFunction function)
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
            string[] file;

            if (!GetFile(symbol.SourceFile).Get(out file)) {
                return GetDefaultSnippet(symbol);
            }

            if (symbol.LineStart >= file.Length) {
                return GetDefaultSnippet(symbol);
            }

            string line = file[symbol.LineStart];

            uint colStart = symbol.ColumnStart;
            uint colEnd = symbol.ColumnEnd;
            
            if (symbol.LineEnd != symbol.LineStart) {
                colEnd = (uint)line.Length - 1;
            }

            if (colEnd >= line.Length) {
                return GetDefaultSnippet(symbol);
            }

            return line.Substring((int)colStart, (int)(colEnd - colStart));
        }

        private string GetDefaultSnippet(Symbol symbol)
        {
            return $"<{symbol.SourceFile}; {symbol.LineStart},{symbol.ColumnStart} : {symbol.LineEnd},{symbol.ColumnEnd}>";
        }

        private Optional<string[]> GetFile(string path)
        {
            if (path == null) {
                return new Optional<string[]>();
            }
            
            string[] file;
            if (_files.TryGetValue(path, out file)) {
                return new Optional<string[]>(file);
            }
            
            try {
                file = File.ReadAllLines(path);
            } catch (Exception) {
                return new Optional<string[]>();
            }
            
            _files[path] = file;
            return new Optional<string[]>(file);
        }
        
    }
}