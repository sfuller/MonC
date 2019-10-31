namespace MonC
{
    public struct Token
    {
        public readonly TokenType Type;
        public readonly string Value;
        public readonly FileLocation Location;

        public Token(TokenType type, string value, FileLocation location)
        {
            Type = type;
            Value = value;
            Location = location;
        }
        
        public override string ToString()
        {
            return $"MonC.Token(Type={Type}, Value=\"{Value}\", Location={Location})";
        }
        
        public readonly FileLocation DeriveEndLocation()
        {
            return new FileLocation {
                Line = Location.Line,
                Column = Location.Column + (uint)Value.Length
            };
        }
    }
}