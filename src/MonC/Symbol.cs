namespace MonC
{
    public struct Symbol
    {
        public ISyntaxTreeNode Node;
        public string? SourceFile;
        public FileLocation Start;
        public FileLocation End;
    }
}