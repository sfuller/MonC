using System.Linq;

namespace MonC
{
    public static class Keyword
    {
        public const string IF = "if";
        public const string ELSE = "else";
        public const string WHILE = "while";
        public const string FOR = "for";
        public const string RETURN = "return";
        public const string STATIC = "static";
        public const string ENUM = "enum";

        private static readonly string[] KEYWORDS = {IF, ELSE, WHILE, FOR, RETURN, STATIC, ENUM};

        public static bool IsKeyword(string value)
        {
            // TODO: PERF
            return KEYWORDS.Contains(value);
        }
    }
}