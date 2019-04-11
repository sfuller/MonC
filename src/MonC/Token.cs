namespace MonC
{
    public struct Token
    {
        public TokenType Type;
        public string Value;

        public uint Line;
        public uint Column;
        
        public override string ToString()
        {
            return $"MonC.Token(Type={Type}, Value=\"{Value}\", Line={Line}, Column={Column})";
        }
        
    }
}