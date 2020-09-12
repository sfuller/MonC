namespace MonC
{
    public struct Symbol
    {
        public ISyntaxTreeLeaf Leaf;
        public string? SourceFile;
        public FileLocation Start;
        public FileLocation End;
    }
}