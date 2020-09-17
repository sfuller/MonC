namespace MonC
{
    public struct Symbol
    {
        public ISyntaxTreeNode Node;
        public string? SourceFile;
        public FileLocation Start;
        public FileLocation End;

        public uint LLVMLine => Start.Line + 1;
        public uint LLVMColumn => Start.Column + 1;
    }
}