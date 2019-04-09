namespace MonC
{
    public struct Token
    {
        public TokenType Type;
        public string Value;


        public override string ToString()
        {
            return $"MonC.Token(Type={Type}, Value={Value})";
        }
        
    }
}