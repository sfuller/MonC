namespace MonC
{
    public struct Symbol
    {
        public IASTLeaf Leaf;
        public string SourceFile;
        public uint LineStart;
        public uint LineEnd;
        public uint ColumnStart;
        public uint ColumnEnd;
    }
}