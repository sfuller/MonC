namespace MonC
{
    public struct Token
    {
        public TokenType Type;
        public string? RawValue;
        public FileLocation Location;

        public Token(TokenType type, string value, FileLocation location)
        {
            Type = type;
            RawValue = value;
            Location = location;
        }

        public readonly string Value => RawValue ?? "";

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
