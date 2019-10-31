namespace MonC
{
    public struct Symbol
    {
        public IASTLeaf Leaf;
        public string? SourceFile;
        public FileLocation Start;
        public FileLocation End;
    }
}